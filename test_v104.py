import requests
import json

url = "https://einvapi.charteredinfo.com/eivital/dec/v1.04/auth"

def test_auth():
    headers = {
        "aspid": "1805053626",
        "password": "Bhavya2026@",
        "Gstin": "24AAICB7094A1ZV",
        "UserName": "API_BPGUJ24",
        "eInvPwd": "Gujarat@123",
        "ip-usr": "127.0.0.1",
        "Accept": "application/json"
    }

    print("\n--- Testing v1.04 E-Invoice Auth ---")
    try:
        response = requests.get(url, headers=headers)
        print(f"Status Code: {response.status_code}")
        try:
            print(f"Response JSON: {json.dumps(response.json(), indent=2)}")
        except Exception:
            print(f"Response Body (Raw): {response.text}")
    except Exception as e:
        print(f"Request failed: {e}")

if __name__ == "__main__":
    test_auth()
