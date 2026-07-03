import requests
import json

url = "https://einvapi.charteredinfo.com/v1.03/dec/auth?action=ACCESSTOKEN"
headers = {
    "aspid": "1805053626",
    "password": "Bhavya2026@",
    "Gstin": "33AAICB7094A1ZW",
    "UserName": "API_BPCHE33",
    "eInvPwd": "Chennai@123",
    "EwbPwd": "Chennai@123"
}
try:
    response = requests.get(url, headers=headers)
    print(f"Status: {response.status_code}")
    print(response.text)
except Exception as e:
    print(e)
