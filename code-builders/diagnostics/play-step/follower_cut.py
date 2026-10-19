"""Bring a follower game to a revision of its record at once, instead of following hundreds of steps a second at a time.

The Play page's follower ("Start following" in the Follow record panel) makes a game `<record>-follow-<n>` that holds the record's first step,
and each "Next step" or "Run" adds the record's next step to it (LivePlay.FollowStep), which checks only that the follower's events are the
record's first events. So once the page has started following, its file may be rewritten with a longer prefix of the record, and the next
step the page takes starts from there.

The cut ends at a step's end: the last event at or before the revision whose next event starts another attempt (ReplaySteps.AttemptOf: the
event id up to its last '-').

Usage: python follower_cut.py <live folder> <record> <follower> <revision>
  e.g. python follower_cut.py src/ASL/boards/units/live/5a7d1f000000400080000000000057d0 cd-guards-5 cd-guards-5-follow-4 714
Then press "Next step" on the page once: the page reads the follower at the cut, plus one step.
"""
import json
import os
import sys

folder, record, follower, revision = sys.argv[1], sys.argv[2], sys.argv[3], int(sys.argv[4])


def attempt(event_id):
    at = event_id.rfind("-")
    return event_id[:at] if at > 0 else event_id


source = json.load(open(os.path.join(folder, record + ".game.json"), encoding="utf-8"))
target_path = os.path.join(folder, follower + ".game.json")
target = json.load(open(target_path, encoding="utf-8"))
events = source["events"]
have = target["events"]
if [e["eventId"] for e in have] != [e["eventId"] for e in events[: len(have)]] or not have:
    sys.exit(f"STOP: {follower} is not a prefix of {record}")

end = min(revision, len(events))
while 0 < end < len(events) and attempt(events[end - 1]["eventId"]) == attempt(events[end]["eventId"]):
    end -= 1
if end < 1:
    sys.exit("STOP: the cut holds no event")
# A cut may also take a follower back: the next step reads the follower's events as they are then.

target["events"] = events[:end]
with open(target_path, "w", encoding="utf-8", newline="\n") as f:
    json.dump(target, f, ensure_ascii=False, indent=2)
print(f"{follower}: {len(have)} -> {end} events (asked {revision}); revision {events[end - 1]['revision']}, next step starts at {events[end]['revision'] if end < len(events) else 'none'}")
