import urllib.request, json, sys
sys.stdout.reconfigure(encoding='utf-8')

login_url = 'http://localhost:5273/api/auth/login'
login_body = json.dumps({'email': 'admin@support.com', 'password': 'Admin@123'}).encode()
req = urllib.request.Request(login_url, data=login_body, headers={'Content-Type': 'application/json'}, method='POST')
with urllib.request.urlopen(req) as resp:
    token = json.loads(resp.read())['token']
print('Login: OK, token acquired')

chat_url = 'http://localhost:5273/api/chat'
tests = [
    'hi',
    'my payment failed',
    'app crash error',
    'forgot my password',
    'some random complex question about warranty',
]

for msg in tests:
    body = json.dumps({'message': msg, 'email': 'admin@support.com'}).encode()
    req = urllib.request.Request(
        chat_url, data=body,
        headers={'Content-Type': 'application/json', 'Authorization': 'Bearer ' + token},
        method='POST'
    )
    try:
        with urllib.request.urlopen(req) as resp:
            r = json.loads(resp.read())
        cat     = r.get('category', '?')
        sent    = r.get('sentiment', '?')
        ticket  = r.get('ticketCreated', '?')
        catconf = r.get('categoryConf', '?')
        reply   = r.get('reply', '')[:70].replace('\n', ' ')
        print(f'MSG: {msg[:30]:<30} | CAT: {cat:<12} ({catconf}%) | SENT: {sent:<10} | TICKET: {ticket} | REPLY: {reply}')
    except Exception as e:
        print(f'MSG: {msg[:30]:<30} | ERROR: {e}')
