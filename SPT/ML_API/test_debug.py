import urllib.request, json, sys
sys.stdout.reconfigure(encoding='utf-8')

login_url = 'http://localhost:5273/api/auth/login'
login_body = json.dumps({'email': 'admin@support.com', 'password': 'Admin@123'}).encode()
req = urllib.request.Request(login_url, data=login_body, headers={'Content-Type': 'application/json'}, method='POST')
with urllib.request.urlopen(req) as resp:
    token = json.loads(resp.read())['token']
print('Login: OK')

chat_url = 'http://localhost:5273/api/chat'

# Single test  
msg = 'My product stopped working after 6 months, what is the warranty coverage?'
body = json.dumps({'message': msg, 'email': 'admin@support.com'}).encode()
req = urllib.request.Request(chat_url, data=body,
    headers={'Content-Type': 'application/json', 'Authorization': 'Bearer ' + token},
    method='POST')
with urllib.request.urlopen(req, timeout=25) as resp:
    r = json.loads(resp.read())

print(json.dumps(r, indent=2, ensure_ascii=False))
