/**
 * 🎵 AudioEngine: Native Web Audio API Chiptune & Arcade Synthesizer + Custom MP3 Support
 *
 * Fully offline-compatible. Generates 8-bit/16-bit melodies and dynamic SFX on the fly,
 * while allowing optional custom MP3 audio files if specified in the configuration.
 */

export type BgmTrack = 'title' | 'running' | 'glitch' | 'challenge' | 'climax' | 'victory' | 'none';

export class AudioEngine {
  private ctx: AudioContext | null = null;
  private masterGain: GainNode | null = null;
  private isMuted: boolean = false;
  private volume: number = 0.7;
  private currentTrack: BgmTrack = 'none';

  // Procedural BGM loop interval / timer
  private bgmTimer: number | null = null;
  private currentStep: number = 0;

  // Custom audio elements (if provided)
  private customBgmAudio: HTMLAudioElement | null = null;
  private customVictoryAudio: HTMLAudioElement | null = null;

  constructor(customBgmUrl?: string, customVictoryUrl?: string, initialVolume = 0.7) {
    this.volume = initialVolume;

    if (customBgmUrl) {
      this.customBgmAudio = new Audio(customBgmUrl);
      this.customBgmAudio.loop = true;
    }
    if (customVictoryUrl) {
      this.customVictoryAudio = new Audio(customVictoryUrl);
    }
  }

  /**
   * Initializes the AudioContext upon first user gesture.
   */
  public init(): void {
    if (!this.ctx) {
      const AudioCtx = window.AudioContext || (window as unknown as { webkitAudioContext: typeof AudioContext }).webkitAudioContext;
      this.ctx = new AudioCtx();
      this.masterGain = this.ctx.createGain();
      this.masterGain.gain.setValueAtTime(this.isMuted ? 0 : this.volume, this.ctx.currentTime);
      this.masterGain.connect(this.ctx.destination);
    }

    if (this.ctx.state === 'suspended') {
      this.ctx.resume();
    }
  }

  public setVolume(vol: number): void {
    this.volume = Math.max(0, Math.min(1, vol));
    if (this.masterGain && this.ctx && !this.isMuted) {
      this.masterGain.gain.setValueAtTime(this.volume, this.ctx.currentTime);
    }
    if (this.customBgmAudio) this.customBgmAudio.volume = this.volume;
    if (this.customVictoryAudio) this.customVictoryAudio.volume = this.volume;
  }

  public toggleMute(): boolean {
    this.isMuted = !this.isMuted;
    if (this.masterGain && this.ctx) {
      this.masterGain.gain.setValueAtTime(this.isMuted ? 0 : this.volume, this.ctx.currentTime);
    }
    if (this.customBgmAudio) this.customBgmAudio.muted = this.isMuted;
    if (this.customVictoryAudio) this.customVictoryAudio.muted = this.isMuted;
    return this.isMuted;
  }

  public getMuted(): boolean {
    return this.isMuted;
  }

  // ==========================================
  // PROCEDURAL BGM ENGINE
  // ==========================================

  public playBgm(track: BgmTrack): void {
    if (this.currentTrack === track) return;
    this.stopBgm();
    this.currentTrack = track;

    if (track === 'none') return;
    this.init();

    // Check custom audio overrides first
    if (track === 'victory' && this.customVictoryAudio) {
      this.customVictoryAudio.currentTime = 0;
      this.customVictoryAudio.play().catch(() => {});
      return;
    }
    if (track === 'running' && this.customBgmAudio) {
      this.customBgmAudio.play().catch(() => {});
      return;
    }

    // Procedural 16-bit synth BGM loop
    this.currentStep = 0;
    const tempoMs = track === 'challenge' ? 120 : track === 'glitch' ? 150 : track === 'victory' ? 140 : 160;

    this.bgmTimer = window.setInterval(() => {
      this.tickBgmStep(track);
      this.currentStep++;
    }, tempoMs);
  }

  public stopBgm(): void {
    if (this.bgmTimer !== null) {
      clearInterval(this.bgmTimer);
      this.bgmTimer = null;
    }
    if (this.customBgmAudio) this.customBgmAudio.pause();
    if (this.customVictoryAudio) this.customVictoryAudio.pause();
    this.currentTrack = 'none';
  }

  private tickBgmStep(track: BgmTrack): void {
    if (!this.ctx || !this.masterGain || this.isMuted) return;

    const step = this.currentStep;

    switch (track) {
      case 'title':
      case 'running': {
        // Cheerful 16-bit arcade chords & bassline
        const bassNotes = [261.63, 293.66, 329.63, 349.23, 392.00, 440.00]; // C4, D4, E4, F4, G4, A4
        const melodyNotes = [523.25, 659.25, 783.99, 1046.50, 880.00, 783.99, 659.25, 587.33];

        if (step % 2 === 0) {
          const bass = bassNotes[(Math.floor(step / 4)) % bassNotes.length];
          this.playTone(bass / 2, 'triangle', 0.18, 0.2);
        }

        const melody = melodyNotes[step % melodyNotes.length];
        this.playTone(melody, 'square', 0.12, 0.12);

        // Hi-hat tick
        if (step % 2 === 1) {
          this.playNoise(0.04, 0.05);
        }
        break;
      }

      case 'glitch': {
        // Tension siren arpeggio
        const tensionNotes = [440, 466.16, 440, 523.25, 587.33, 622.25];
        const note = tensionNotes[step % tensionNotes.length];
        this.playTone(note, 'sawtooth', 0.1, 0.2);
        if (step % 4 === 0) {
          this.playTone(110, 'sawtooth', 0.25, 0.25);
        }
        break;
      }

      case 'challenge': {
        // High-energy fast double-time beat
        const fastBass = [130.81, 146.83, 164.81, 196.00];
        this.playTone(fastBass[step % fastBass.length], 'sawtooth', 0.1, 0.25);
        const fastLead = [523.25, 783.99, 659.25, 1046.50];
        this.playTone(fastLead[(step * 2) % fastLead.length], 'square', 0.08, 0.15);
        this.playNoise(0.05, 0.08);
        break;
      }

      case 'climax': {
        // Suspense drum roll and rising harmonics
        const freq = 200 + (step % 16) * 35;
        this.playTone(freq, 'sawtooth', 0.09, 0.15);
        this.playNoise(0.06, 0.1);
        break;
      }

      case 'victory': {
        // Triumphant Fanfare (Major Triad Celebration)
        const fanfare = [
          523.25, 523.25, 523.25, 659.25, 783.99, 1046.50,
          880.00, 1046.50, 1174.66, 1318.51, 1567.98
        ];
        const note = fanfare[step % fanfare.length];
        this.playTone(note, 'square', 0.22, 0.25);
        this.playTone(note / 2, 'triangle', 0.25, 0.25);
        if (step % 3 === 0) {
          this.playNoise(0.12, 0.15); // Snare / firework pop
        }
        break;
      }
    }
  }

  // ==========================================
  // PROCEDURAL SOUND EFFECTS (SFX)
  // ==========================================

  /**
   * Jump sound (rising square/triangle frequency)
   */
  public playJump(): void {
    this.init();
    if (!this.ctx || !this.masterGain || this.isMuted) return;

    const osc = this.ctx.createOscillator();
    const gain = this.ctx.createGain();
    const now = this.ctx.currentTime;

    osc.type = 'square';
    osc.frequency.setValueAtTime(150, now);
    osc.frequency.exponentialRampToValueAtTime(600, now + 0.15);

    gain.gain.setValueAtTime(0.25, now);
    gain.gain.linearRampToValueAtTime(0.01, now + 0.15);

    osc.connect(gain);
    gain.connect(this.masterGain);

    osc.start(now);
    osc.stop(now + 0.16);
  }

  /**
   * Hobby / Coin / Item pickup sound
   */
  public playCoin(): void {
    this.init();
    if (!this.ctx || !this.masterGain || this.isMuted) return;

    const now = this.ctx.currentTime;
    this.playToneAtTime(987.77, 'square', 0.08, 0.18, now); // B5
    this.playToneAtTime(1318.51, 'square', 0.22, 0.22, now + 0.08); // E6
  }

  /**
   * Pet greeting sound (Bark or Meow)
   */
  public playPetSound(type: 'dog' | 'cat'): void {
    this.init();
    if (!this.ctx || !this.masterGain || this.isMuted) return;

    const now = this.ctx.currentTime;
    const osc = this.ctx.createOscillator();
    const gain = this.ctx.createGain();

    if (type === 'dog') {
      // Bark: punchy down-pitch
      osc.type = 'sawtooth';
      osc.frequency.setValueAtTime(320, now);
      osc.frequency.exponentialRampToValueAtTime(140, now + 0.12);
      gain.gain.setValueAtTime(0.3, now);
      gain.gain.linearRampToValueAtTime(0.01, now + 0.13);
    } else {
      // Meow: rising and falling slide
      osc.type = 'triangle';
      osc.frequency.setValueAtTime(450, now);
      osc.frequency.exponentialRampToValueAtTime(750, now + 0.15);
      osc.frequency.exponentialRampToValueAtTime(500, now + 0.3);
      gain.gain.setValueAtTime(0.25, now);
      gain.gain.linearRampToValueAtTime(0.01, now + 0.3);
    }

    osc.connect(gain);
    gain.connect(this.masterGain);
    osc.start(now);
    osc.stop(now + (type === 'dog' ? 0.14 : 0.31));
  }

  /**
   * Rubber Ducky squeak (for Troll Block!)
   */
  public playDuckySqueak(): void {
    this.init();
    if (!this.ctx || !this.masterGain || this.isMuted) return;

    const now = this.ctx.currentTime;
    const osc = this.ctx.createOscillator();
    const gain = this.ctx.createGain();

    osc.type = 'sine';
    osc.frequency.setValueAtTime(700, now);
    osc.frequency.exponentialRampToValueAtTime(1400, now + 0.09);
    osc.frequency.exponentialRampToValueAtTime(1100, now + 0.18);

    gain.gain.setValueAtTime(0.35, now);
    gain.gain.linearRampToValueAtTime(0.01, now + 0.2);

    osc.connect(gain);
    gain.connect(this.masterGain);
    osc.start(now);
    osc.stop(now + 0.21);
  }

  /**
   * Block Punch Impact
   */
  public playBlockHit(hitIndex: number): void {
    this.init();
    if (!this.ctx || !this.masterGain || this.isMuted) return;

    const now = this.ctx.currentTime;
    const baseFreq = 160 + hitIndex * 60;

    // Metallic chime
    const osc = this.ctx.createOscillator();
    const gain = this.ctx.createGain();
    osc.type = 'triangle';
    osc.frequency.setValueAtTime(baseFreq, now);
    osc.frequency.exponentialRampToValueAtTime(baseFreq * 2.5, now + 0.1);

    gain.gain.setValueAtTime(0.3, now);
    gain.gain.exponentialRampToValueAtTime(0.01, now + 0.2);

    osc.connect(gain);
    gain.connect(this.masterGain);
    osc.start(now);
    osc.stop(now + 0.22);

    // Punch thump
    this.playNoise(0.08, 0.2 + hitIndex * 0.04);
  }

  /**
   * Glitch zap sound
   */
  public playGlitchZap(): void {
    this.init();
    if (!this.ctx || !this.masterGain || this.isMuted) return;

    const now = this.ctx.currentTime;
    const osc = this.ctx.createOscillator();
    const gain = this.ctx.createGain();

    osc.type = 'sawtooth';
    osc.frequency.setValueAtTime(800, now);
    osc.frequency.setValueAtTime(200, now + 0.04);
    osc.frequency.setValueAtTime(1200, now + 0.08);

    gain.gain.setValueAtTime(0.28, now);
    gain.gain.linearRampToValueAtTime(0.01, now + 0.15);

    osc.connect(gain);
    gain.connect(this.masterGain);
    osc.start(now);
    osc.stop(now + 0.16);
  }

  /**
   * Firework rocket & explosion sound
   */
  public playFirework(): void {
    this.init();
    if (!this.ctx || !this.masterGain || this.isMuted) return;

    // Pop & rumble
    const now = this.ctx.currentTime;
    this.playToneAtTime(550, 'sine', 0.1, 0.15, now);
    setTimeout(() => {
      this.playNoise(0.3, 0.35);
    }, 120);
  }

  /**
   * Cheerful cartoon car horn (bibi!)
   */
  public playCarHorn(): void {
    this.init();
    if (!this.ctx || !this.masterGain || this.isMuted) return;

    const now = this.ctx.currentTime;
    // Dual-tone harmonic horn (F5 + A5)
    this.playToneAtTime(698.46, 'triangle', 0.14, 0.28, now);
    this.playToneAtTime(880.0, 'triangle', 0.14, 0.24, now);
    this.playToneAtTime(698.46, 'triangle', 0.18, 0.28, now + 0.18);
    this.playToneAtTime(880.0, 'triangle', 0.18, 0.24, now + 0.18);
  }

  /**
   * Engine rev sound (Vroom!)
   */
  public playEngineRev(): void {
    this.init();
    if (!this.ctx || !this.masterGain || this.isMuted) return;

    const now = this.ctx.currentTime;
    const osc = this.ctx.createOscillator();
    const gain = this.ctx.createGain();

    osc.type = 'sawtooth';
    osc.frequency.setValueAtTime(65, now);
    osc.frequency.exponentialRampToValueAtTime(240, now + 0.35);
    osc.frequency.exponentialRampToValueAtTime(140, now + 0.7);

    gain.gain.setValueAtTime(0.22, now);
    gain.gain.linearRampToValueAtTime(0.01, now + 0.7);

    osc.connect(gain);
    gain.connect(this.masterGain);
    osc.start(now);
    osc.stop(now + 0.72);
  }

  /**
   * Cartoon spring bounce sound (Boing!)
   */
  public playSpringBounce(): void {
    this.init();
    if (!this.ctx || !this.masterGain || this.isMuted) return;

    const now = this.ctx.currentTime;
    const osc = this.ctx.createOscillator();
    const gain = this.ctx.createGain();

    osc.type = 'sine';
    osc.frequency.setValueAtTime(180, now);
    osc.frequency.exponentialRampToValueAtTime(800, now + 0.22);

    gain.gain.setValueAtTime(0.35, now);
    gain.gain.exponentialRampToValueAtTime(0.01, now + 0.3);

    osc.connect(gain);
    gain.connect(this.masterGain);
    osc.start(now);
    osc.stop(now + 0.32);
  }

  /**
   * Rocket nitro boost woosh sound
   */
  public playRocketBoost(): void {
    this.init();
    if (!this.ctx || !this.masterGain || this.isMuted) return;

    const now = this.ctx.currentTime;
    const osc = this.ctx.createOscillator();
    const gain = this.ctx.createGain();

    osc.type = 'sawtooth';
    osc.frequency.setValueAtTime(200, now);
    osc.frequency.exponentialRampToValueAtTime(900, now + 0.5);

    gain.gain.setValueAtTime(0.25, now);
    gain.gain.linearRampToValueAtTime(0.01, now + 0.55);

    osc.connect(gain);
    gain.connect(this.masterGain);
    osc.start(now);
    osc.stop(now + 0.58);

    this.playNoise(0.4, 0.2);
  }

  /**
   * Tire screech sound
   */
  public playTireScreech(): void {
    this.init();
    if (!this.ctx || !this.masterGain || this.isMuted) return;

    const now = this.ctx.currentTime;
    const osc = this.ctx.createOscillator();
    const gain = this.ctx.createGain();

    osc.type = 'triangle';
    osc.frequency.setValueAtTime(900, now);
    osc.frequency.linearRampToValueAtTime(1400, now + 0.15);
    osc.frequency.linearRampToValueAtTime(1100, now + 0.3);

    gain.gain.setValueAtTime(0.2, now);
    gain.gain.linearRampToValueAtTime(0.01, now + 0.3);

    osc.connect(gain);
    gain.connect(this.masterGain);
    osc.start(now);
    osc.stop(now + 0.32);
  }

  // ==========================================
  // PRIMITIVE TONE HELPERS
  // ==========================================

  private playTone(freq: number, type: OscillatorType, duration: number, gainVol: number): void {
    if (!this.ctx) return;
    this.playToneAtTime(freq, type, duration, gainVol, this.ctx.currentTime);
  }

  private playToneAtTime(freq: number, type: OscillatorType, duration: number, gainVol: number, time: number): void {
    if (!this.ctx || !this.masterGain || this.isMuted) return;

    const osc = this.ctx.createOscillator();
    const gain = this.ctx.createGain();

    osc.type = type;
    osc.frequency.setValueAtTime(freq, time);

    gain.gain.setValueAtTime(gainVol, time);
    gain.gain.exponentialRampToValueAtTime(0.001, time + duration);

    osc.connect(gain);
    gain.connect(this.masterGain);

    osc.start(time);
    osc.stop(time + duration);
  }

  private playNoise(duration: number, gainVol: number): void {
    if (!this.ctx || !this.masterGain || this.isMuted) return;

    const bufferSize = Math.floor(this.ctx.sampleRate * duration);
    const buffer = this.ctx.createBuffer(1, bufferSize, this.ctx.sampleRate);
    const data = buffer.getChannelData(0);

    for (let i = 0; i < bufferSize; i++) {
      data[i] = Math.random() * 2 - 1;
    }

    const noise = this.ctx.createBufferSource();
    noise.buffer = buffer;

    const gain = this.ctx.createGain();
    const now = this.ctx.currentTime;
    gain.gain.setValueAtTime(gainVol, now);
    gain.gain.linearRampToValueAtTime(0.001, now + duration);

    noise.connect(gain);
    gain.connect(this.masterGain);

    noise.start(now);
  }
}
