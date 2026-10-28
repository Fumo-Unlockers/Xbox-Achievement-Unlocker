import json
import unittest
from validate_template_privacy import revealing, inspect_payload

class PrivacyGuardTests(unittest.TestCase):
    def test_capture_identifiers_are_rejected(self):
        self.assertTrue(revealing('UserID', '1234567890123456'))
        self.assertTrue(revealing('PlayerSessionId', '12345678-1234-1234-1234-123456789abc'))
        self.assertTrue(revealing('username', 'example-user'))
        self.assertTrue(revealing('path', 'C:\\Users\\Example\\capture.json'))
        self.assertTrue(revealing('token', 'Bearer example'))

    def test_runtime_and_metadata_values_are_retained(self):
        self.assertFalse(revealing('UserID', 'REPLACEXUID'))
        self.assertFalse(revealing('PlayerSessionId', 'REPLACESESSIONGUID'))
        self.assertFalse(revealing('PlayerSessionId', 4))  # Metadata type annotation.
        self.assertFalse(revealing('serviceConfigId', '12345678-1234-1234-1234-123456789abc'))
        self.assertFalse(revealing('PlayerSessionId', '00000000-0000-0000-0000-000000000000'))

    def test_embedded_payload_is_checked_without_echoing_values(self):
        captured = '1234567890123456'
        errors = []
        inspect_payload({'Replacement': json.dumps({'baseData': {'userId': captured}})}, 'file', errors)
        self.assertEqual(errors, ['file/Replacement/baseData/userId'])
        self.assertNotIn(captured, '\n'.join(errors))

if __name__ == '__main__':
    unittest.main()
