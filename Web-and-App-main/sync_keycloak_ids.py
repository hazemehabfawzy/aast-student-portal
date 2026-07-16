"""
Syncs Keycloak user UUIDs into the local SQLite DB by matching on email address.
Safe to re-run at any time.
"""
import sqlite3, urllib.request, urllib.parse, json, os

keycloak_url = os.environ.get('KEYCLOAK_URL', 'http://localhost:8080').rstrip('/')

# ── 1. Get admin token ────────────────────────────────────────────────────────
data = urllib.parse.urlencode({
    'grant_type': 'password',
    'client_id':  'admin-cli',
    'username':   'admin',
    'password':   'admin'
}).encode()

req = urllib.request.Request(
    f'{keycloak_url}/realms/master/protocol/openid-connect/token', data=data)
token = json.loads(urllib.request.urlopen(req).read())['access_token']

# ── 2. Get all users from Keycloak ────────────────────────────────────────────
req2 = urllib.request.Request(
    f'{keycloak_url}/admin/realms/student-portal/users?max=100',
    headers={'Authorization': f'Bearer {token}'})
users = json.loads(urllib.request.urlopen(req2).read())

print(f'Keycloak users found: {len(users)}')
for u in users:
    print(f"  {u['username']}  email={u.get('email', '(none)')}  id={u['id']}")

# Build email → uuid map
email_to_uuid = {u['email'].lower(): u['id'] for u in users if u.get('email')}
print(f'\nEmail->UUID map built: {len(email_to_uuid)} entries')

# ── 3. Connect to DB ──────────────────────────────────────────────────────────
db = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'data', 'student_portal.db')
conn = sqlite3.connect(db)
c = conn.cursor()

updated_students    = 0
updated_instructors = 0
skipped             = 0

# ── 4. Sync Students (match by Email column) ──────────────────────────────────
print('\nSyncing Students by email:')
for row in c.execute('SELECT Id, FullName, Email, KeycloakId FROM Students').fetchall():
    sid, name, email, old_id = row
    if not email:
        print(f'  SKIP (no email): {name}')
        skipped += 1
        continue
    new_id = email_to_uuid.get(email.lower())
    if new_id and new_id != old_id:
        c.execute('UPDATE Students SET KeycloakId = ? WHERE Id = ?', (new_id, sid))
        print(f'  UPDATED {name} ({email}): {old_id} -> {new_id}')
        updated_students += 1
    elif new_id == old_id:
        print(f'  OK (already correct): {name}')
    else:
        print(f'  NO MATCH in Keycloak for email: {email}  ({name})')
        skipped += 1

# ── 5. Sync Instructors (match by Email if column exists) ─────────────────────
print('\nSyncing Instructors:')
try:
    instructors = c.execute('SELECT Id, FullName, Email, KeycloakId FROM Instructors').fetchall()
    for row in instructors:
        iid, name, email, old_id = row
        if not email:
            print(f'  SKIP (no email): {name}')
            skipped += 1
            continue
        new_id = email_to_uuid.get(email.lower())
        if new_id and new_id != old_id:
            c.execute('UPDATE Instructors SET KeycloakId = ? WHERE Id = ?', (new_id, iid))
            print(f'  UPDATED {name} ({email}): {old_id} -> {new_id}')
            updated_instructors += 1
        elif new_id == old_id:
            print(f'  OK (already correct): {name}')
        else:
            print(f'  NO MATCH in Keycloak for email: {email}  ({name})')
            skipped += 1
except Exception as ex:
    # Fallback: match by username pattern against FullName
    print(f'  Email column unavailable ({ex}), falling back to name matching')
    for user in users:
        username = user['username']
        new_id = user['id']
        name_part = username.replace('.', ' ').title()
        result = c.execute(
            'UPDATE Instructors SET KeycloakId = ? WHERE LOWER(FullName) LIKE ?',
            (new_id, f'%{name_part.lower()}%'))
        if result.rowcount > 0:
            print(f'  UPDATED Instructor via name: {username} -> {new_id}')
            updated_instructors += result.rowcount

conn.commit()

print(f'\n=== Sync complete ===')
print(f'Students  updated : {updated_students}')
print(f'Instructors updated: {updated_instructors}')
print(f'Skipped           : {skipped}')

# ── 6. Final verification ─────────────────────────────────────────────────────
print('\nDB after sync:')
for row in c.execute('SELECT FullName, Email, KeycloakId FROM Students'):
    print(f'  Student    {row[0]} ({row[1]}): {row[2]}')
try:
    for row in c.execute('SELECT FullName, Email, KeycloakId FROM Instructors'):
        print(f'  Instructor {row[0]} ({row[1]}): {row[2]}')
except Exception:
    for row in c.execute('SELECT FullName, KeycloakId FROM Instructors'):
        print(f'  Instructor {row[0]}: {row[1]}')

conn.close()
