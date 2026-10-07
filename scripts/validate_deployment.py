"""Validate deployment structure; this does not build or run Docker images."""

import json
from pathlib import Path
from urllib.request import urlopen

import jsonschema
import yaml

ROOT = Path(__file__).resolve().parents[1]
SCHEMAS = {
    "render.yaml": "https://render.com/schema/render.yaml.json",
    "docker-compose.yml": "https://raw.githubusercontent.com/compose-spec/compose-spec/main/schema/compose-spec.json",
}


def main() -> None:
    documents = {}
    for name, url in SCHEMAS.items():
        with urlopen(url, timeout=30) as response:
            schema = json.load(response)
        with (ROOT / name).open(encoding="utf-8-sig") as source:
            document = yaml.safe_load(source)
        jsonschema.validators.validator_for(schema)(schema).validate(document)
        documents[name] = document
        print(f"PASS {name}: JSON Schema")

    render = documents["render.yaml"]
    compose = documents["docker-compose.yml"]
    expected = {"equilibrafit-plusplus-api", "equilibrafit-plusplus-admin", "equilibrafit-plusplus-ai"}
    assert {service["name"] for service in render["services"]} == expected
    assert set(compose["services"]) == expected | {"redis"}
    assert "databases" not in render
    assert compose["services"]["redis"]["profiles"] == ["cache"]
    for service in render["services"]:
        assert service["runtime"] == "docker"
        assert (ROOT / service["dockerfilePath"]).is_file()
        assert (ROOT / service.get("dockerContext", ".")).is_dir()
        for variable in service.get("envVars", []):
            if "fromService" in variable:
                assert variable["fromService"]["name"] in expected
    api = next(service for service in render["services"] if service["name"].endswith("-api"))
    variables = {variable["key"]: variable for variable in api["envVars"]}
    assert variables["SUPABASE_DB_CONNECTION_STRING"]["sync"] is False
    assert variables["DATABASE_PROVIDER"]["value"] == "PostgreSQL"
    assert variables["RUN_DB_MIGRATIONS"]["value"] == "true"
    print("PASS services, Docker paths, secret references, remote DB and optional Redis")


if __name__ == "__main__":
    main()
