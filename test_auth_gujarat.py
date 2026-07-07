import requests
import json

url = "https://einvapi.charteredinfo.com/v1.03/dec/auth?action=ACCESSTOKEN"

def test_auth(gstin, username, password, include_ewb_pwd):
    headers = {
        "aspid": "1805053626",
        "password": "Bhavya2026@",
        "Gstin": gstin,
        "UserName": username,
        "eInvPwd": password,
        "ip-usr": "127.0.0.1",
        "Accept": "application/json"
    }

    if include_ewb_pwd:
        headers["EwbPwd"] = password
        print(f"\n--- Testing {gstin} WITH EwbPwd ---")
    else:
        print(f"\n--- Testing {gstin} WITHOUT EwbPwd ---")
    
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
    # Gujarat Credentials
    test_auth("24AAICB7094A1ZV", "API_BPGUJ24", "Gujarat@123", True)
    test_auth("24AAICB7094A1ZV", "API_BPGUJ24", "Gujarat@123", False)
