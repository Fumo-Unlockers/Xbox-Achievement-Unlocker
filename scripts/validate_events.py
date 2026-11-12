"""Reject research/housekeeping fields in the runtime event catalog. No networking."""
import json
import pathlib
import re
import sys

TITLE_KEYS = {'TitleId', 'TitleName', 'Devices', 'ServiceConfigId', 'Enabled', 'FullySupported', 'Achievements'}
OP_KEYS = {'ReplacementType', 'Target', 'Replacement', 'Min', 'Max'}
OP_TYPES = {'Replace', 'RangeInt', 'RangeFloat', 'StupidFuckingLDAPTimestamp'}

def unique_pairs(items):
    result = {}
    for key, value in items:
        if key in result:
            raise ValueError(f'Duplicate key: {key}')
        result[key] = value
    return result

def validate(data, directory):
    assert list(data)[-1] == 'SupportedTitleIDs', 'SupportedTitleIDs must be last'
    ids = data['SupportedTitleIDs']
    assert isinstance(ids, list) and len(ids) == len(set(ids)), 'Invalid supported IDs'
    for tid, title in data.items():
        if tid == 'SupportedTitleIDs':
            continue
        assert tid.isdigit() and isinstance(title, dict), f'Invalid title {tid}'
        assert not set(title) - TITLE_KEYS, f'{tid}: non-runtime title fields {set(title)-TITLE_KEYS}'
        achievements = title.get('Achievements')
        external = directory / 'Catalog' / f'{tid}.json'
        if external.exists():
            assert achievements == {}, f'{tid}: conflicting inline and external mappings'
            achievements = json.loads(external.read_text(encoding='utf-8-sig'), object_pairs_hook=unique_pairs)
        assert isinstance(achievements, dict), f'{tid}: missing Achievements'
        for aid, entry in achievements.items():
            path = f'{tid}/{aid}'
            assert aid.isdigit() and isinstance(entry, dict) and entry, f'{path}: invalid mapping'
            if any(re.fullmatch(r'Request\d+', k) for k in entry):
                assert list(entry) == [f'Request{i}' for i in range(1, len(entry)+1)], f'{path}: invalid requests'
                requests = entry.values()
            else:
                requests = [entry]
            for request in requests:
                assert isinstance(request, dict) and request, f'{path}: empty request'
                for label, op in request.items():
                    assert isinstance(op, dict) and not set(op)-OP_KEYS, f'{path}/{label}: non-runtime operation'
                    kind = op.get('ReplacementType')
                    assert kind in OP_TYPES, f'{path}/{label}: unknown replacement type'
                    assert isinstance(op.get('Target'), str) and op['Target'], f'{path}/{label}: missing target'
                    if kind == 'Replace':
                        # Existing XAU accepts numeric replacement scalars as well as strings.
                        assert isinstance(op.get('Replacement'), (str, int, float, bool)), f'{path}/{label}: invalid replacement'
                    if kind in ('RangeInt', 'RangeFloat'):
                        assert isinstance(op.get('Min'), (int, float)) and isinstance(op.get('Max'), (int, float)), f'{path}/{label}: invalid range'
                        assert op['Min'] <= op['Max'], f'{path}/{label}: reversed range'
    for tid in ids:
        assert str(tid) in data, f'Missing title block {tid}'
        assert (directory / f'{tid}.json').is_file(), f'Missing template {tid}'
        achievements = data[str(tid)]['Achievements']
        external = directory / 'Catalog' / f'{tid}.json'
        if external.exists():
            achievements = json.loads(external.read_text(encoding='utf-8-sig'), object_pairs_hook=unique_pairs)
        assert achievements, f'Listed title {tid} has no achievement mappings'

if __name__ == '__main__':
    directory = pathlib.Path(sys.argv[1] if len(sys.argv) > 1 else 'Events')
    raw = (directory / 'Data.json').read_bytes()
    assert b'\n' not in raw.replace(b'\r\n', b''), 'Data.json must use CRLF'
    data = json.loads(raw.decode('utf-8-sig'), object_pairs_hook=unique_pairs)
    validate(data, directory)
    print(f'Strict schema passed: {len(data)-1} titles. Run the C# renderer to validate every achievement payload.')
