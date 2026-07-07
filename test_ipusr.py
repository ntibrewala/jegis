import requests
import json

url = "https://einvapi.charteredinfo.com/v1.03/dec/auth?action=ACCESSTOKEN"

def test_auth():
    headers = {
        "aspid": "1805053626",
        "password": "Bhavya2026@",
        "Gstin": "24AAICB7094A1ZV",
        "UserName": "API_BPGUJ24",
        "eInvPwd": "Gujarat@123",
        "EwbPwd": "Gujarat@123",
        "Accept": "application/json"
    }

    print("\n--- Testing WITHOUT ip-usr ---")
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
