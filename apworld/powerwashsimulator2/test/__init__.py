from test.bases import WorldTestBase
from settings import get_settings


class PWS2TestBase(WorldTestBase):
    game = "PowerWash Simulator 2"
    # host.yaml settings this test class needs; the rest stay at their defaults
    host_settings: dict = {}

    def setUp(self) -> None:
        self.apply_host_settings()
        super().setUp()

    def apply_host_settings(self) -> None:
        group = get_settings()["powerwashsimulator2_options"]
        self._saved_settings = {key: getattr(group, key) for key in self.host_settings}
        for key, value in self.host_settings.items():
            setattr(group, key, value)

    def tearDown(self) -> None:
        group = get_settings()["powerwashsimulator2_options"]
        for key, value in self._saved_settings.items():
            setattr(group, key, value)
        super().tearDown()
