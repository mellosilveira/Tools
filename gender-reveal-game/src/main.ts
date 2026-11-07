/**
 * 🚀 Main Bootstrapper: Application Entry Point
 *
 * Wire up GameEngine, AudioEngine, CoopCharSelectOverlay, CustomizerModal, and UI listeners.
 */

import './style.css';
import { loadGameConfig, saveGameConfig, OutfitTheme } from './config/gameConfig';
import { AudioEngine } from './audio/AudioEngine';
import { GameEngine } from './engine/GameEngine';
import { CustomizerModal } from './ui/CustomizerModal';
import { CoopCharSelectOverlay } from './ui/CoopCharSelectOverlay';
import { getTranslations } from './config/i18n';

window.addEventListener('DOMContentLoaded', () => {
  const canvas = document.getElementById('game-canvas') as HTMLCanvasElement;
  if (!canvas) throw new Error('Game canvas element not found');

  // 1. Load Configuration
  let config = loadGameConfig();

  // 2. Instantiate Audio & Game Engines
  const audio = new AudioEngine(config.customAudio.bgmUrl, config.customAudio.victoryUrl, config.customAudio.volume);
  const engine = new GameEngine(canvas, config, audio);

  // 3. Instantiate Arcade Co-op Character Selection Overlay
  const coopSelect = new CoopCharSelectOverlay(
    config,
    (updatedConfig) => {
      config = updatedConfig;
      engine.setConfig(updatedConfig);
      updateThemePills();
    },
    () => {
      engine.startAnimation();
    }
  );

  // 4. Instantiate Secret Customizer Modal
  const customizer = new CustomizerModal(config, (newConfig) => {
    config = newConfig;
    engine.setConfig(newConfig);
    coopSelect.setConfig(newConfig);
    updateThemePills();
    updateUiText();
  });

  // Auto-open customizer if ?admin=1 in URL
  const urlParams = new URLSearchParams(window.location.search);
  if (urlParams.get('admin') === '1') {
    customizer.open();
  }

  // 5. UI Elements Wiring
  const openSkinsBtn = document.getElementById('btn-open-skins');
  const skinsLabel = document.getElementById('skins-label');
  const langBtn = document.getElementById('btn-lang-toggle');
  const langLabel = document.getElementById('lang-label');
  const soundBtn = document.getElementById('btn-sound-toggle');
  const soundIcon = document.getElementById('sound-icon');
  const fullscreenBtn = document.getElementById('btn-fullscreen-toggle');
  const adminBtn = document.getElementById('btn-admin-gear');
  const finaleBar = document.getElementById('finale-actions');
  const replayBtn = document.getElementById('btn-finale-replay');
  const fireworksBtn = document.getElementById('btn-finale-fireworks');

  // Title Quick Actions
  const titleActionBar = document.getElementById('title-action-bar');
  const titleMoreSkinsBtn = document.getElementById('btn-title-more-skins');
  const titleStartBtn = document.getElementById('btn-title-start');
  const quickThemeButtons = document.querySelectorAll('[data-quick-theme]');

  function updateThemePills(): void {
    const currentTheme = config.parents.dad.outfitTheme || 'casual';
    quickThemeButtons.forEach((btn) => {
      const theme = (btn as HTMLElement).dataset.quickTheme;
      if (theme === currentTheme) {
        btn.classList.add('active');
      } else {
        btn.classList.remove('active');
      }
    });
  }

  function updateUiText(): void {
    const isPt = config.language === 'pt';
    const t = getTranslations(config.language);
    if (langLabel) langLabel.textContent = isPt ? 'PT' : 'EN';
    if (skinsLabel) skinsLabel.textContent = isPt ? 'Skins Co-op' : 'Co-op Skins';
    if (replayBtn) replayBtn.textContent = `↺ ${t.replayButton}`;
    if (fireworksBtn) fireworksBtn.textContent = `🎆 ${t.fireworksButton}`;
    if (titleMoreSkinsBtn) titleMoreSkinsBtn.textContent = isPt ? '🎨 Escolher Skins P1 & P2' : '🎨 Choose Skins (P1 & P2)';
    if (titleStartBtn) titleStartBtn.textContent = isPt ? '▶ INICIAR ANIMAÇÃO 🚀' : '▶ START ANIMATION 🚀';
    document.title = isPt ? 'A Grande Corrida da Família - Animação Chá Revelação' : 'The Great Family Race - Gender Reveal Animation';
  }

  updateUiText();
  updateThemePills();

  // Quick Theme Buttons on Title Screen
  quickThemeButtons.forEach((btn) => {
    btn.addEventListener('click', (e) => {
      e.stopPropagation();
      const theme = (btn as HTMLElement).dataset.quickTheme as OutfitTheme;
      if (theme) {
        engine.setOutfitTheme(theme);
        config = loadGameConfig();
        coopSelect.setConfig(config);
        saveGameConfig(config);
        updateThemePills();
      }
    });
  });

  // Title screen co-op selection button
  titleMoreSkinsBtn?.addEventListener('click', (e) => {
    e.stopPropagation();
    coopSelect.open();
  });

  // Title screen start button -> Launches Animation directly
  titleStartBtn?.addEventListener('click', (e) => {
    e.stopPropagation();
    engine.startAnimation();
  });

  // Top HUD Skins Button
  openSkinsBtn?.addEventListener('click', (e) => {
    e.stopPropagation();
    coopSelect.open();
  });

  // Language Toggle
  langBtn?.addEventListener('click', (e) => {
    e.stopPropagation();
    config.language = config.language === 'pt' ? 'en' : 'pt';
    saveGameConfig(config);
    engine.setConfig(config);
    coopSelect.setConfig(config);
    updateUiText();
  });

  // Sound Toggle
  soundBtn?.addEventListener('click', (e) => {
    e.stopPropagation();
    const muted = audio.toggleMute();
    if (soundIcon) soundIcon.textContent = muted ? '🔇' : '🔊';
  });

  // Fullscreen Toggle
  fullscreenBtn?.addEventListener('click', (e) => {
    e.stopPropagation();
    if (!document.fullscreenElement) {
      document.documentElement.requestFullscreen().catch(() => {});
    } else {
      document.exitFullscreen().catch(() => {});
    }
  });

  // Admin Modal Toggle
  adminBtn?.addEventListener('click', (e) => {
    e.stopPropagation();
    customizer.toggle();
  });

  // Finale Buttons
  replayBtn?.addEventListener('click', (e) => {
    e.stopPropagation();
    engine.restartGame();
    if (finaleBar) finaleBar.style.display = 'none';
  });

  fireworksBtn?.addEventListener('click', (e) => {
    e.stopPropagation();
    engine.handleAction();
  });

  // Canvas Action (Click / Tap)
  canvas.addEventListener('pointerdown', () => {
    if (engine.getState() === 'TITLE_SCREEN') {
      engine.startAnimation();
      return;
    }
    engine.handleAction();
  });

  // Keyboard Shortcuts
  window.addEventListener('keydown', (e) => {
    if (e.code === 'KeyS') {
      coopSelect.open();
      return;
    }
    if (e.code === 'KeyC') {
      customizer.toggle();
      return;
    }
    if (e.code === 'KeyR') {
      engine.restartGame();
      if (finaleBar) finaleBar.style.display = 'none';
      return;
    }
    if (e.code === 'KeyM') {
      const muted = audio.toggleMute();
      if (soundIcon) soundIcon.textContent = muted ? '🔇' : '🔊';
      return;
    }
    if (e.code === 'KeyF') {
      if (!document.fullscreenElement) {
        document.documentElement.requestFullscreen().catch(() => {});
      } else {
        document.exitFullscreen().catch(() => {});
      }
      return;
    }

    if (e.code === 'Space' || e.code === 'Enter') {
      e.preventDefault();
      if (coopSelect.isVisible()) {
        coopSelect.close();
        engine.startAnimation();
        return;
      }
      if (engine.getState() === 'TITLE_SCREEN') {
        engine.startAnimation();
        return;
      }
      engine.handleAction();
    }
  });

  // Observer for Title Screen bar & Grand Finale button visibility
  setInterval(() => {
    const state = engine.getState();

    // Title action bar
    if (state === 'TITLE_SCREEN') {
      if (titleActionBar && titleActionBar.style.display === 'none') {
        titleActionBar.style.display = 'flex';
      }
    } else {
      if (titleActionBar && titleActionBar.style.display !== 'none') {
        titleActionBar.style.display = 'none';
      }
    }

    // Finale bar
    if (state === 'GRAND_FINALE') {
      if (finaleBar && finaleBar.style.display === 'none') {
        finaleBar.style.display = 'flex';
      }
    } else {
      if (finaleBar && finaleBar.style.display !== 'none') {
        finaleBar.style.display = 'none';
      }
    }
  }, 100);

  // 6. Start Game Loop
  engine.start();
});
