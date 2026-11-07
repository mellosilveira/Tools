/**
 * 🗺️ TrackElevation: Dynamic Terrain Elevation & Slope Engine (Mario & Sonic Style)
 *
 * Provides organic, smooth, platformer terrain heights and slopes:
 * - Decouples the physical ground polygon from vehicle stunt trajectories.
 * - Ground surface: Smooth continuous hills using smooth composite trigonometric harmonics.
 * - Vehicle trajectory: Rides on ground, enters 360° Sonic Loop, leaps over Ducky, glides across Cloud Chasm.
 */

export interface TrackPoint {
  y: number; // Ground surface Y coordinate
  slope: number; // Ground slope in radians
  zone: 'green_hill' | 'sonic_loop' | 'cyber_tunnel' | 'castle' | 'victory';
}

export interface VehiclePoint {
  y: number; // Cart chassis Y coordinate
  slope: number; // Cart tilt angle in radians
  isAirborne: boolean;
  isLooping: boolean;
  zone: 'green_hill' | 'sonic_loop' | 'cyber_tunnel' | 'castle' | 'victory';
}

/**
 * Returns the ground surface point for worldX.
 * Guarantees smooth, continuous organic platformer terrain (no jarring spikes or bumps!).
 */
export function getTrackPoint(worldX: number, groundBaseY: number): TrackPoint {
  // -----------------------------------------------------------------
  // 1. ZONE 1: Green Hill & Mushroom Valley (0 -> 4500)
  // -----------------------------------------------------------------
  if (worldX < 4500) {
    // Smooth, gentle rolling hills inspired by Super Mario World & Sonic 1
    const w1 = worldX * 0.0015;
    const w2 = worldX * 0.0032;
    const hillY = Math.sin(w1) * 20 + Math.sin(w2) * 8;
    const slope = Math.cos(w1) * (20 * 0.0015) + Math.cos(w2) * (8 * 0.0032);

    return {
      y: groundBaseY + hillY,
      slope: Math.max(-0.2, Math.min(0.2, slope)),
      zone: 'green_hill'
    };
  }

  // -----------------------------------------------------------------
  // 2. ZONE 2: Sonic Sunset Shore & Corkscrew Loops (4500 -> 9000)
  // -----------------------------------------------------------------
  if (worldX >= 4500 && worldX < 9000) {
    // Smooth coastal highway.
    // The ground is flat under the Sonic Loop (5100 -> 5700) so the loop stands proudly on the road!
    let coastalY = 0;
    let slope = 0;

    if (worldX >= 5100 && worldX <= 5700) {
      // Flat runway plaza for the 360 loop
      coastalY = 0;
      slope = 0;
    } else if (worldX >= 7300 && worldX <= 7900) {
      // Flat seaside pier / boardwalk where Ducky and Spring Ramp sit
      coastalY = 4;
      slope = 0;
    } else {
      // Gentle scenic coastal undulation
      const w = worldX * 0.0014;
      coastalY = Math.sin(w) * 14;
      slope = Math.cos(w) * (14 * 0.0014);
    }

    return {
      y: groundBaseY + coastalY,
      slope: Math.max(-0.16, Math.min(0.16, slope)),
      zone: 'sonic_loop'
    };
  }

  // -----------------------------------------------------------------
  // 3. ZONE 3: Cyberpunk Mag-Rail Bridge & Cloud Chasm (9000 -> 13500)
  // -----------------------------------------------------------------
  if (worldX >= 9000 && worldX < 13500) {
    const bridgeBase = groundBaseY - 28;

    return {
      y: bridgeBase,
      slope: 0,
      zone: 'cyber_tunnel'
    };
  }

  // -----------------------------------------------------------------
  // 4. ZONE 4: Bowser's Volcanic Castle & Mega Mystery Block (13500 -> 17500)
  // -----------------------------------------------------------------
  if (worldX >= 13500 && worldX < 17500) {
    // Castle approach ramp with gentle incline
    const castleWave = Math.sin(worldX * 0.0012) * 12;
    const slope = Math.cos(worldX * 0.0012) * (12 * 0.0012);

    return {
      y: groundBaseY + castleWave,
      slope: Math.max(-0.15, Math.min(0.15, slope)),
      zone: 'castle'
    };
  }

  // -----------------------------------------------------------------
  // 5. ZONE 5: Winner's Runway & 1st Place Podium (17500+)
  // -----------------------------------------------------------------
  return {
    y: groundBaseY,
    slope: 0,
    zone: 'victory'
  };
}

/**
 * Returns the vehicle track point for worldX.
 * Coordinates vehicle stunt physics: 360° Sonic Loop, Ducky Spring Jump, Cloud Rocket Chasm.
 */
export function getVehiclePoint(worldX: number, groundBaseY: number): VehiclePoint {
  const ground = getTrackPoint(worldX, groundBaseY);

  // A. Sonic 360° Loop (worldX 5200 -> 5600)
  if (worldX >= 5200 && worldX <= 5600) {
    const loopP = (worldX - 5200) / 400; // 0.0 to 1.0
    const loopRadius = 110;
    const loopHeight = Math.sin(loopP * Math.PI) * (loopRadius * 2);
    // Tilt angle starts at 0, turns upward, inverts at crest (upside down!), and descends smoothly
    const loopTilt = (loopP - 0.5) * Math.PI * 1.85;

    return {
      y: ground.y - loopHeight,
      slope: loopTilt,
      isAirborne: loopHeight > 30,
      isLooping: true,
      zone: ground.zone
    };
  }

  // B. Super Spring Parabolic Jump over Giant Ducky (worldX 7400 -> 7820)
  if (worldX >= 7400 && worldX <= 7820) {
    const jumpP = (worldX - 7400) / 420; // 0.0 to 1.0
    const jumpHeight = Math.sin(jumpP * Math.PI) * 195;
    // Aerial stunt tilt
    const jumpTilt = -Math.cos(jumpP * Math.PI) * 0.35;

    return {
      y: ground.y - jumpHeight,
      slope: jumpTilt,
      isAirborne: jumpHeight > 10,
      isLooping: false,
      zone: ground.zone
    };
  }

  // C. Cloud Rocket Chasm Flight (worldX 10600 -> 11500)
  if (worldX >= 10600 && worldX <= 11500) {
    const rocketP = (worldX - 10600) / 900; // 0.0 to 1.0
    const rocketGlide = Math.sin(rocketP * Math.PI) * 130;
    const rocketTilt = (rocketP - 0.5) * 0.22;

    return {
      y: ground.y - rocketGlide,
      slope: rocketTilt,
      isAirborne: rocketGlide > 15,
      isLooping: false,
      zone: ground.zone
    };
  }

  // Normal ground contact
  return {
    y: ground.y,
    slope: ground.slope,
    isAirborne: false,
    isLooping: false,
    zone: ground.zone
  };
}
