/**
 * 🎮 Gender Reveal Game Configuration
 *
 * Central configuration file for the 2D Arcade & Cartoon HD Gender Reveal Game.
 */

export type Language = 'pt' | 'en';
export type Gender = 'boy' | 'girl';
export type PetType = 'dog' | 'cat' | 'none';
export type HairStyleMom = 'long' | 'bun' | 'curly' | 'short';
export type HairStyleDad = 'short' | 'fade' | 'curly' | 'bald';
export type OutfitTheme = 'casual' | 'cyberpunk' | 'dragon_knight';

export type HobbyType =
  | 'gamepad'
  | 'dumbbell'
  | 'soccer'
  | 'book'
  | 'music'
  | 'coffee'
  | 'camera'
  | 'cooking';

export interface ParentMomConfig {
  name: string;
  skinTone: string;
  hairColor: string;
  hairStyle: HairStyleMom;
  outfitTheme: OutfitTheme;
  outfitColor: string;
}

export interface ParentDadConfig {
  name: string;
  skinTone: string;
  hairColor: string;
  hairStyle: HairStyleDad;
  outfitTheme: OutfitTheme;
  hasBeard: boolean;
  hasGlasses: boolean;
  outfitColor: string;
}

export interface PetConfig {
  enabled: boolean;
  type: PetType;
  name: string;
  primaryColor: string;
  secondaryColor: string;
  outfitTheme: OutfitTheme;
}

export interface ThemeColorsConfig {
  boyPrimary: string;
  boySecondary: string;
  boyGlow: string;
  girlPrimary: string;
  girlSecondary: string;
  girlGlow: string;
  arcadeGold: string;
  arcadePurple: string;
}

export interface CustomAudioConfig {
  bgmUrl?: string;
  victoryUrl?: string;
  volume: number;
}

export interface GameConfig {
  language: Language;
  revealGender: Gender;
  autoAssistTimeoutMs: number;
  parents: {
    mom: ParentMomConfig;
    dad: ParentDadConfig;
  };
  pet: PetConfig;
  hobbies: HobbyType[];
  themeColors: ThemeColorsConfig;
  customAudio: CustomAudioConfig;
}

export const DEFAULT_CONFIG: GameConfig = {
  language: 'pt',
  revealGender: 'girl',
  autoAssistTimeoutMs: 7000,
  parents: {
    mom: {
      name: 'Mamãe',
      skinTone: '#fcd5b5',
      hairColor: '#4a2c11',
      hairStyle: 'long',
      outfitTheme: 'casual',
      outfitColor: '#ec4899'
    },
    dad: {
      name: 'Papai',
      skinTone: '#f8d1b0',
      hairColor: '#2b1d0c',
      hairStyle: 'fade',
      outfitTheme: 'casual',
      hasBeard: true,
      hasGlasses: false,
      outfitColor: '#3b82f6'
    }
  },
  pet: {
    enabled: true,
    type: 'dog',
    name: 'Pipoca',
    primaryColor: '#eab308',
    secondaryColor: '#fef08a',
    outfitTheme: 'casual'
  },
  hobbies: ['gamepad', 'dumbbell', 'coffee', 'music'],
  themeColors: {
    boyPrimary: '#00d2ff',
    boySecondary: '#3b82f6',
    boyGlow: 'rgba(0, 210, 255, 0.8)',
    girlPrimary: '#ff2a85',
    girlSecondary: '#ec4899',
    girlGlow: 'rgba(255, 42, 133, 0.8)',
    arcadeGold: '#fbbf24',
    arcadePurple: '#a855f7'
  },
  customAudio: {
    volume: 0.75
  }
};

const STORAGE_KEY = 'gender_reveal_game_custom_config_v2';

export function loadGameConfig(): GameConfig {
  try {
    const urlParams = new URLSearchParams(window.location.search);
    const cfgParam = urlParams.get('cfg');
    if (cfgParam) {
      const decodedJson = atob(cfgParam);
      const parsed = JSON.parse(decodedJson) as Partial<GameConfig>;
      return mergeWithDefault(parsed);
    }

    const saved = localStorage.getItem(STORAGE_KEY);
    if (saved) {
      const parsed = JSON.parse(saved) as Partial<GameConfig>;
      return mergeWithDefault(parsed);
    }
  } catch (err) {
    console.warn('Failed to parse saved or URL game configuration. Falling back to defaults.', err);
  }

  return JSON.parse(JSON.stringify(DEFAULT_CONFIG));
}

export function saveGameConfig(config: GameConfig): void {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(config));
  } catch (err) {
    console.error('Failed to save configuration to localStorage', err);
  }
}

export function encodeConfigToUrl(config: GameConfig): string {
  try {
    const jsonStr = JSON.stringify(config);
    return btoa(jsonStr);
  } catch (err) {
    console.error('Failed to encode config to Base64', err);
    return '';
  }
}

function mergeWithDefault(partial: Partial<GameConfig>): GameConfig {
  const def = JSON.parse(JSON.stringify(DEFAULT_CONFIG)) as GameConfig;
  return {
    ...def,
    ...partial,
    parents: {
      mom: {
        ...def.parents.mom,
        ...(partial.parents?.mom || {}),
        outfitTheme: partial.parents?.mom?.outfitTheme || def.parents.mom.outfitTheme || 'casual'
      },
      dad: {
        ...def.parents.dad,
        ...(partial.parents?.dad || {}),
        outfitTheme: partial.parents?.dad?.outfitTheme || def.parents.dad.outfitTheme || 'casual'
      }
    },
    pet: {
      ...def.pet,
      ...(partial.pet || {}),
      outfitTheme: partial.pet?.outfitTheme || def.pet.outfitTheme || 'casual'
    },
    hobbies: partial.hobbies || def.hobbies,
    themeColors: { ...def.themeColors, ...(partial.themeColors || {}) },
    customAudio: { ...def.customAudio, ...(partial.customAudio || {}) }
  };
}
