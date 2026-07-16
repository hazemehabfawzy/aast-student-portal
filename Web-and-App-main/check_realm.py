import json, os
script_dir = os.path.dirname(os.path.abspath(__file__))
with open(os.path.join(script_dir, 'keycloak', 'realm-export.json'), 'r') as f:
    realm = json.load(f)
users = realm.get('users', [])
print(f'Users in realm-export.json: {len(users)}')
for u in users:
    uid = u.get('id')
    username = u.get('username')
    email = u.get('email')
    print(f'  username={username}  id={uid}  email={email}')
