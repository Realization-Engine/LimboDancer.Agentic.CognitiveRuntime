"use strict";
// Generated snapshot of ASL codebase values. Estimates are explicitly identified.
window.SquadAslDefinitions = {
 "version": "asl-stlo-1",
 "catalogVersion": "1.13.0",
 "sourceSha256": "c6388e9a8c41a4359d58033831020a38bff434a6c22c86f9879924087376a54f",
 "types": {
  "us-rifle": {
   "source": "src/ASL/units/catalog/scenario-a1.catalog.json",
   "definitionId": "american-squad",
   "status": "catalog match",
   "values": {
    "firepower": 6,
    "range": 6,
    "morale": 6,
    "class": "1st-line",
    "smoke-exponent": 3
   }
  },
  "de-rifle": {
   "source": "src/ASL/units/catalog/scenario-a1.catalog.json",
   "definitionId": "attacker-squad",
   "status": "catalog match",
   "values": {
    "firepower": 4,
    "range": 6,
    "morale": 7,
    "class": "1st-line",
    "smoke-exponent": 1
   }
  },
  "de-engineer": {
   "source": "src/ASL/units/catalog/scenario-a1.catalog.json",
   "definitionId": "attacker-elite-squad-8-3-8",
   "status": "catalog match",
   "values": {
    "firepower": 8,
    "range": 3,
    "morale": 8,
    "class": "elite",
    "smoke-exponent": 3
   }
  },
  "de-truck": {
   "source": "src/ASL/units/catalog/scenario-a1.catalog.json",
   "definitionId": "attacker-truck",
   "status": "catalog match",
   "values": {
    "designation": "Opel 6700 (Blitz)",
    "movement-type": "truck",
    "movement-points": 28,
    "towing": 7,
    "passenger-capacity": 21
   }
  },
  "de-halftrack": {
   "source": "src/ASL/units/catalog/scenario-a1.catalog.json",
   "definitionId": "attacker-halftrack",
   "status": "catalog match",
   "values": {
    "designation": "SPW 251/1",
    "movement-type": "half-tracked",
    "movement-points": 16,
    "af-front": 1,
    "af-side": 1,
    "target-size": "small",
    "ma-weapon": "aamg",
    "rate-of-fire": 1,
    "breakdown": 12,
    "towing": 7,
    "passenger-capacity": 15,
    "aamg": 3
   }
  },
  "us-mg": {
   "source": "src/ASL/units/catalog/scenario-a1.catalog.json",
   "definitionId": "attacker-hmg",
   "status": "provisional estimate",
   "rationale": "American heavy MG approximation from catalog HMG.",
   "values": {
    "size": "heavy",
    "firepower": 8,
    "range": 16,
    "breakdown": 12,
    "rate-of-fire": 3,
    "portage": 5
   }
  },
  "us-57mm": {
   "source": "src/ASL/units/examples/catalog.units.json",
   "definitionId": "example-at-gun",
   "status": "provisional estimate",
   "rationale": "Lighter AT gun based on example PaK 40.",
   "values": {
    "designation": "57 mm AT",
    "gun-type": "at",
    "caliber": 57,
    "caliber-suffix": "l",
    "rate-of-fire": 3,
    "manhandling": 8,
    "special-ammo": [
     "A4",
     "H6"
    ]
   }
  },
  "de-75mm": {
   "source": "src/ASL/units/examples/catalog.units.json",
   "definitionId": "example-at-gun",
   "status": "provisional estimate",
   "rationale": "Existing ASL example PaK 40 is synthetic, so values remain provisional.",
   "values": {
    "designation": "PaK 40",
    "gun-type": "at",
    "caliber": 75,
    "caliber-suffix": "l",
    "rate-of-fire": 2,
    "manhandling": 8,
    "special-ammo": [
     "A4",
     "H6"
    ]
   }
  },
  "de-20mm": {
   "source": "src/ASL/units/examples/catalog.units.json",
   "definitionId": "example-aa-gun",
   "status": "provisional estimate",
   "rationale": "20 mm AA example used for German gun pending catalog match.",
   "values": {
    "gun-type": "aa",
    "caliber": 20,
    "caliber-suffix": "l",
    "rate-of-fire": 3,
    "ife": 4,
    "manhandling": 12,
    "target-size": "small",
    "traits": [
     "asl:mount-360"
    ]
   }
  },
  "us-81mm": {
   "source": "src/ASL/units/examples/catalog.units.json",
   "definitionId": "example-mortar-gun",
   "status": "provisional estimate",
   "rationale": "82 mm mortar example adapted to 81 mm; crew included.",
   "values": {
    "gun-type": "mtr",
    "caliber": 81,
    "rate-of-fire": 3,
    "range-minimum": 3,
    "range-maximum": 60,
    "manhandling": 11,
    "target-size": "small",
    "special-ammo": [
     "S8",
     "WP7"
    ],
    "traits": [
     "asl:no-ap"
    ]
   }
  },
  "de-81mm": {
   "source": "src/ASL/units/examples/catalog.units.json",
   "definitionId": "example-mortar-gun",
   "status": "provisional estimate",
   "rationale": "82 mm mortar example adapted to 81 mm; crew included.",
   "values": {
    "gun-type": "mtr",
    "caliber": 81,
    "rate-of-fire": 3,
    "range-minimum": 3,
    "range-maximum": 60,
    "manhandling": 11,
    "target-size": "small",
    "special-ammo": [
     "S8",
     "WP7"
    ],
    "traits": [
     "asl:no-ap"
    ]
   }
  },
  "de-120mm": {
   "source": "src/ASL/units/examples/catalog.units.json",
   "definitionId": "example-mortar-gun",
   "status": "provisional estimate",
   "rationale": "Heavy mortar estimate; lower rate of fire and mobility.",
   "values": {
    "gun-type": "mtr",
    "caliber": 120,
    "rate-of-fire": 2,
    "range-minimum": 3,
    "range-maximum": 60,
    "manhandling": 8,
    "target-size": "small",
    "special-ammo": [
     "S8",
     "WP7"
    ],
    "traits": [
     "asl:no-ap"
    ]
   }
  },
  "us-m4-75": {
   "source": "src/ASL/units/examples/catalog.units.json",
   "definitionId": "example-tank",
   "status": "provisional estimate",
   "rationale": "Playable vehicle estimate based on existing ASL tank example; no Panzer factors copied.",
   "values": {
    "designation": "us-m4-75",
    "identity": "A",
    "movement-type": "fully-tracked",
    "movement-points": 13,
    "af-front": 8,
    "af-side": 3,
    "ma-type": "t",
    "caliber": 75,
    "caliber-suffix": "l",
    "rate-of-fire": 1,
    "special-ammo": [
     "A4",
     "S7"
    ],
    "bmg": 2,
    "cmg": 4
   }
  },
  "us-m4-76": {
   "source": "src/ASL/units/examples/catalog.units.json",
   "definitionId": "example-tank",
   "status": "provisional estimate",
   "rationale": "Playable vehicle estimate based on existing ASL tank example; no Panzer factors copied.",
   "values": {
    "designation": "us-m4-76",
    "identity": "A",
    "movement-type": "fully-tracked",
    "movement-points": 13,
    "af-front": 8,
    "af-side": 3,
    "ma-type": "t",
    "caliber": 76,
    "caliber-suffix": "l",
    "rate-of-fire": 1,
    "special-ammo": [
     "A4",
     "S7"
    ],
    "bmg": 2,
    "cmg": 4
   }
  },
  "us-m4-105": {
   "source": "src/ASL/units/examples/catalog.units.json",
   "definitionId": "example-tank",
   "status": "provisional estimate",
   "rationale": "Playable vehicle estimate based on existing ASL tank example; no Panzer factors copied.",
   "values": {
    "designation": "us-m4-105",
    "identity": "A",
    "movement-type": "fully-tracked",
    "movement-points": 13,
    "af-front": 8,
    "af-side": 3,
    "ma-type": "t",
    "caliber": 105,
    "caliber-suffix": "star",
    "rate-of-fire": 1,
    "special-ammo": [
     "A4",
     "S7"
    ],
    "bmg": 2,
    "cmg": 4
   }
  },
  "us-m5": {
   "source": "src/ASL/units/examples/catalog.units.json",
   "definitionId": "example-tank",
   "status": "provisional estimate",
   "rationale": "Playable vehicle estimate based on existing ASL tank example; no Panzer factors copied.",
   "values": {
    "designation": "us-m5",
    "identity": "A",
    "movement-type": "fully-tracked",
    "movement-points": 18,
    "af-front": 4,
    "af-side": 3,
    "ma-type": "t",
    "caliber": 37,
    "caliber-suffix": "l",
    "rate-of-fire": 1,
    "special-ammo": [
     "A4",
     "S7"
    ],
    "bmg": 2,
    "cmg": 4
   }
  },
  "de-hetzer": {
   "source": "src/ASL/units/examples/catalog.units.json",
   "definitionId": "example-tank",
   "status": "provisional estimate",
   "rationale": "Playable vehicle estimate based on existing ASL tank example; no Panzer factors copied.",
   "values": {
    "designation": "de-hetzer",
    "identity": "A",
    "movement-type": "fully-tracked",
    "movement-points": 13,
    "af-front": 11,
    "af-side": 3,
    "ma-type": "t",
    "caliber": 75,
    "caliber-suffix": "l",
    "rate-of-fire": 1,
    "special-ammo": [
     "A4",
     "S7"
    ],
    "bmg": 2,
    "cmg": 4
   }
  },
  "de-stug": {
   "source": "src/ASL/units/examples/catalog.units.json",
   "definitionId": "example-tank",
   "status": "provisional estimate",
   "rationale": "Playable vehicle estimate based on existing ASL tank example; no Panzer factors copied.",
   "values": {
    "designation": "de-stug",
    "identity": "A",
    "movement-type": "fully-tracked",
    "movement-points": 14,
    "af-front": 8,
    "af-side": 3,
    "ma-type": "t",
    "caliber": 75,
    "caliber-suffix": "l",
    "rate-of-fire": 1,
    "special-ammo": [
     "A4",
     "S7"
    ],
    "bmg": 2,
    "cmg": 4
   }
  },
  "us-halftrack": {
   "source": "src/ASL/units/catalog/scenario-a1.catalog.json",
   "definitionId": "attacker-halftrack",
   "status": "provisional estimate",
   "rationale": "Comparable halftrack catalog values.",
   "values": {
    "designation": "M3 halftrack",
    "movement-type": "half-tracked",
    "movement-points": 16,
    "af-front": 1,
    "af-side": 1,
    "target-size": "small",
    "ma-weapon": "aamg",
    "rate-of-fire": 1,
    "breakdown": 12,
    "towing": 7,
    "passenger-capacity": 15,
    "aamg": 3
   }
  },
  "us-truck": {
   "source": "src/ASL/units/examples/catalog.units.json",
   "definitionId": "example-truck",
   "status": "provisional estimate",
   "rationale": "American truck example is synthetic; retained provisionally.",
   "values": {
    "designation": "2.5-ton truck",
    "identity": "E",
    "movement-type": "truck",
    "movement-points": 30,
    "target-size": "very-small",
    "towing": 10,
    "passenger-capacity": 9,
    "traits": [
     "asl:unarmored"
    ]
   }
  },
  "de-wagon": {
   "source": "src/ASL/units/catalog/scenario-a1.catalog.json",
   "definitionId": "attacker-truck",
   "status": "provisional estimate",
   "rationale": "Horse transport approximation with reduced movement and capacity.",
   "values": {
    "designation": "Wagon",
    "movement-type": "wagon",
    "movement-points": 8,
    "towing": 0,
    "passenger-capacity": 10
   }
  }
 }
};
