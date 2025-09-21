from .level import Level
from .metric import Metric


class RoomCount(Metric):
    """Measures count of rooms, relative to maximum possible number of rooms (the whole level filled with 1x1 rooms)"""

    @classmethod
    def title(cls) -> str:
        return "Room Count"

    def score(self, level: Level) -> float:
        return level.num_rooms / level.total_size
