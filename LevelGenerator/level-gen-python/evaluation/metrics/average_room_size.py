from .level import Level
from .metric import Metric


class AverageRoomSize(Metric):
    """Measures average size of rooms, relative to the maximum allowed size"""

    def __init__(self, max_size: int):
        self._max_size = max_size

    @classmethod
    def title(cls) -> str:
        return "Average Room Size"

    def score(self, level: Level) -> float:
        return sum(rm.w * rm.h for rm in level.rooms) / (level.num_rooms * self._max_size)
