import sqlite3
import urllib.request
import json

# Get admin token
req = urllib.request.Request(
    'http://localhost:8080/realms/master/protocol/openid-connect/token',
    data=b'grant_type=password&client_id=admin-cli&username=admin&password=admin',
    headers={'Content-Type': 'application/x-www-form-urlencoded'}
)
with urllib.request.urlopen(req) as r:
    token = json.loads(r.read())['access_token']

# Get all users
req = urllib.request.Request(
    'http://localhost:8080/admin/realms/student-portal/users?max=50',
    headers={'Authorization': f'Bearer {token}'}
)
with urllib.request.urlopen(req) as r:
    users = json.loads(r.read())

# Build username -> UUID map
user_map = {u['username']: u['id'] for u in users}
print("Users found:", list(user_map.keys()))

# Map to database records
student_numbers = {
    'student.one':   '19104001',
    'student.two':   '19104002',
    'student.three': '19104003',
    'student.four':  '19104004',
    'student.five':  '19104005',
}

instructor_names = {
    'instructor.one': 'Ahmed Hassan',
    'instructor.two': 'Instructor Two',
}

conn = sqlite3.connect(
    r'd:\projects\StudentPortal\Web-and-App-main\data\student_portal.db'
)
c = conn.cursor()

# Update students
for username, student_number in student_numbers.items():
    if username in user_map:
        kid = user_map[username]
        rows = c.execute(
            'UPDATE Students SET KeycloakId=? WHERE StudentNumber=?',
            (kid, student_number)
        ).rowcount
        print(f"Student {student_number}: updated {rows} rows with {kid[:8]}...")

# Update instructors
for username, name in instructor_names.items():
    if username in user_map:
        kid = user_map[username]
        rows = c.execute(
            'UPDATE Instructors SET KeycloakId=? WHERE FullName=?',
            (kid, name)
        ).rowcount
        print(f"Instructor {name}: updated {rows} rows with {kid[:8]}...")

conn.commit()

# Verify
print("\n=== Verification ===")
for row in c.execute('SELECT StudentNumber, FullName, KeycloakId FROM Students'):
    print(f"  {row[0]} | {row[1]} | {row[2][:8]}...")

conn.close()
print("Done")
