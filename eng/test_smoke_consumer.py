import tempfile
import unittest
import xml.etree.ElementTree as ET

from pathlib import Path

from smoke_consumer import write_nuget_config


class IsolatedNuGetConfigurationTests(unittest.TestCase):
    def test_registers_local_feed_and_official_upstream(self):
        with tempfile.TemporaryDirectory() as directory:
            work = Path(directory)
            local_feed = work / "local"
            path = work / "NuGet.Config"

            write_nuget_config(path, local_feed)

            root = ET.parse(path).getroot()
            sources = root.find("packageSources")
            self.assertIsNotNone(sources)
            self.assertIsNotNone(sources.find("clear"))
            observed = {
                item.attrib["key"]: item.attrib["value"]
                for item in sources.findall("add")
            }
            self.assertEqual(str(local_feed), observed["local"])
            self.assertEqual("https://api.nuget.org/v3/index.json", observed["nuget.org"])


if __name__ == "__main__":
    unittest.main()
