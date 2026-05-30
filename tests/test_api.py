import requests

def test_healthcheck():
    r = requests.get("http://localhost:3000/health")
    assert r.status_code == 200
