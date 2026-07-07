import requests
import json

url = "https://einvapi.charteredinfo.com/v1.03/dec/auth?action=ACCESSTOKEN"

def test_auth(include_ewb_pwd):
    headers = {
        "aspid": "1805053626",
        "password": "Bhavya2026@",
        "Gstin": "19AAICB7094A1ZM",
        "UserName": "BPPLkolkat_API_019",
        "eInvPwd": "Kolkata@123",
        "ip-usr": "127.0.0.1",
        "Accept": "application/json"
    }

    if include_ewb_pwd:
        headers["EwbPwd"] = "Kolkata@123"
        print("\n--- Testing WITH EwbPwd (The buggy way) ---")
    else:
        print("\n--- Testing WITHOUT EwbPwd (The fixed way) ---")

    print(f"Headers sent: {json.dumps(headers, indent=2)}")
    
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
    # Test the buggy way first (should return 400)
    test_auth(include_ewb_pwd=True)
    
    # Test the fixed way (should return 200 and a token)
    test_auth(include_ewb_pwd=False)
