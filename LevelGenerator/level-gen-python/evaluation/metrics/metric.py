from abc import ABC, abstractmethod

from .level import Level


class Metric(ABC):

    @classmethod
    @abstractmethod
    def title(cls) -> str:
        """Title of metric"""

    @abstractmethod
    def score(self, level: Level) -> float:
        """Returns the metrics score for the given level

        Should (ideally) be normalised between 0 and 1, but this is not enforced.
        """
