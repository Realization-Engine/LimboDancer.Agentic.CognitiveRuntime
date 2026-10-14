"""Pass 45, the reviews: a game still in setup on board 3, the Russians the Scenario Defender, to set a MMG up by hand with its Bore Sighted
Location (C6.41, C6.42) on the page.

Usage: python make_setup.py <game id>
"""
import sys

from p45lib import Game

game = Game(sys.argv[1], "Pass 45 reviews: a MMG set up by hand with its Bore Sighted Location (C6.41, C6.42; board 3)", defender="russian")
game.write()
