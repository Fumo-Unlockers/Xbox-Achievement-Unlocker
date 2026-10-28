"""Offline privacy guard. Errors identify fields, never their captured values."""
import json
import pathlib
import re
import sys

STRING = r'"(?:\\.|[^"\\])*"'
VALUE = STRING + r'|-?\d+(?:\.\d+)?|true|false|null'
NEUTRAL = {'shellId': 1, 'epoch': '1', 'bootId': 1, 'expId': '1',
           'devMake': '1', 'devModel': '1', 'tz': '00:00', 'asId': 1}
LOCAL_IDS = {'m:1111111111111111', 's:11111111-1111-1111-1111-111111111111'}

def revealing(key, value):
    if not isinstance(value, (str, int)):
        return False
    text = str(value)
    if 'REPLACE' in text or set(text) <= set('01-:ms'):
        return False
    if key.lower() in ('userid', 'xuid', 'playerid', 'accountid') and re.fullmatch(r'\d{10,}', text):
        return True
    if isinstance(value, str) and key.lower() in ('playersessionid', 'sessionid', 'lifeid', 'correlationid', 'activityid', 'installationid') and text:
        return True
    if isinstance(value, str) and key.lower() in ('gamertag', 'email', 'username', 'hostname', 'computername', 'machinename', 'ipaddress', 'macaddress', 'serialnumber', 'windowsuserid', 'puid', 'authorization', 'token', 'access_token', 'refresh_token', 'ticket') and text:
        return True
    if re.search(r'[A-Z]:\\', text, re.I) and text != 'C:\\Anonymous\\file':
        return True
    if re.search(r'[\w.+-]+@[\w.-]+\.[A-Za-z]{2,}', text) and text != 'anonymous@example.invalid':
        return True
    return bool(re.search(r'XBL3\.0|Bearer\s|eyJ[A-Za-z0-9_-]{15,}', text))

def inspect_payload(obj, path, errors):
    if isinstance(obj, dict):
        for key, value in obj.items():
            child = path + '/' + key
            if revealing(key, value):
                errors.append(child)
            if key == 'Replacement' and isinstance(value, str) and value.lstrip().startswith(('{', '[')):
                try:
                    inspect_payload(json.loads(value), child, errors)
                except ValueError:
                    pass  # Payload rendering validates fragment replacements separately.
            elif isinstance(value, (dict, list)):
                inspect_payload(value, child, errors)
    elif isinstance(obj, list):
        for value in obj:
            inspect_payload(value, path + '[]', errors)

def main(directory):
    errors = []
    templates = [p for p in directory.glob('*.json') if p.stem.isdigit()]
    for p in templates:
        text = p.read_text(encoding='utf-8-sig')
        for match in re.finditer('(' + STRING + r')(\s*:\s*)(' + VALUE + ')', text):
            key, value = json.loads(match.group(1)), json.loads(match.group(3))
            if key in NEUTRAL and value != NEUTRAL[key]:
                errors.append(p.name + '/' + key)
            if key == 'localId' and value not in LOCAL_IDS:
                errors.append(p.name + '/localId')
            if revealing(key, value):
                errors.append(p.name + '/' + key)
        for block in re.findall(r'"os"\s*:\s*\{([^{}]*)\}', text):
            for version in re.findall(r'"ver"\s*:\s*(' + STRING + ')', block):
                if json.loads(version) != '1':
                    errors.append(p.name + '/ext.os.ver')
        for tickets in re.findall(r'"ticketKeys"\s*:\s*(\[[^\[\]]*\])', text):
            if json.loads(tickets) != ['1']:
                errors.append(p.name + '/ext.protocol.ticketKeys')
    for p in [directory / 'Data.json', *(directory / 'Catalog').glob('*.json')]:
        inspect_payload(json.loads(p.read_text(encoding='utf-8-sig')), p.name, errors)
    if errors:
        raise SystemExit('Potentially identifying fields require review:\n' + '\n'.join(sorted(set(errors))))
    print(f'Privacy guard passed: {len(templates)} standalone templates and their recipe payloads.')

if __name__ == '__main__':
    main(pathlib.Path(sys.argv[1] if len(sys.argv) > 1 else 'Events'))
