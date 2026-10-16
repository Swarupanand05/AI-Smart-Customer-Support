import urllib.request, json, sys
sys.stdout.reconfigure(encoding='utf-8')

login_url = 'http://localhost:5273/api/auth/login'
login_body = json.dumps({'email': 'admin@support.com', 'password': 'Admin@123'}).encode()
req = urllib.request.Request(login_url, data=login_body, headers={'Content-Type': 'application/json'}, method='POST')
with urllib.request.urlopen(req) as resp:
    token = json.loads(resp.read())['token']
print('Login: OK')

chat_url = 'http://localhost:5273/api/chat'

# These should hit Gemini (no FAQ match)
gemini_tests = [
    'My product stopped working after 6 months, what is the warranty coverage?',
    'How do I export my data from your platform?',
    'Can I use your service in multiple countries simultaneously?',
]

for msg in gemini_tests:
    body = json.dumps({'message': msg, 'email': 'admin@support.com'}).encode()
    req = urllib.request.Request(chat_url, data=body,
        headers={'Content-Type': 'application/json', 'Authorization': 'Bearer ' + token},
        method='POST')
    try:
        with urllib.request.urlopen(req, timeout=20) as resp:
            r = json.loads(resp.read())
        print()
        print(f'Q: {msg[:60]}')
        print(f'Category: {r.get("category")} ({r.get("categoryConf")}%) | Sentiment: {r.get("sentiment")} ({r.get("sentimentConf")}%)')
        print(f'Ticket: {r.get("ticketCreated")} | Priority: {r.get("priority")}')
        print(f'Reply: {r.get("reply","")[:200]}')
    except Exception as e:
        print(f'ERROR for [{msg[:40]}]: {e}')
