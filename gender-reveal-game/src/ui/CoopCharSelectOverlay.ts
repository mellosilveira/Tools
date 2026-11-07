/**
 * 🎮 CoopCharSelectOverlay: Arcade Co-op Character Selection Screen (Full HD)
 *
 * Provides a split-podium player select screen:
 * - PLAYER 1 (MAMÃE): Live animated portrait canvas, independent theme selection, colors, hair.
 * - PLAYER 2 (PAPAI): Live animated portrait canvas, independent theme selection, colors, hair, beard, glasses.
 * - SQUAD PET: Center companion slot with wings/visor/bandana customization.
 * - START CO-OP ADVENTURE button!
 */

import { GameConfig, HairStyleMom, HairStyleDad, PetType, OutfitTheme, saveGameConfig } from '../config/gameConfig';
import { CharacterRenderer, CharacterAnimationState } from '../entities/CharacterRenderer';
import { getTranslations } from '../config/i18n';

export class CoopCharSelectOverlay {
  private container: HTMLElement;
  private config: GameConfig;
  private onConfigChanged: (newConfig: GameConfig) => void;
  private onStartGame: () => void;
  private isOpen: boolean = false;
  private previewAnimFrame: number | null = null;

  // Live preview animation states
  private p1State: CharacterAnimationState;
  private p2State: CharacterAnimationState;
  private petState: CharacterAnimationState;
  private animTime: number = 0;

  // Canvas elements
  private p1Canvas: HTMLCanvasElement | null = null;
  private p2Canvas: HTMLCanvasElement | null = null;
  private petCanvas: HTMLCanvasElement | null = null;

  // Player Ready Status
  private p1Ready: boolean = false;
  private p2Ready: boolean = false;

  constructor(
    initialConfig: GameConfig,
    onConfigChanged: (newConfig: GameConfig) => void,
    onStartGame: () => void
  ) {
    this.config = JSON.parse(JSON.stringify(initialConfig));
    this.onConfigChanged = onConfigChanged;
    this.onStartGame = onStartGame;

    this.p1State = {
      x: 90,
      y: 190,
      vx: 0,
      vy: 0,
      isGrounded: true,
      isJumping: false,
      runCycle: 0,
      squashStretchX: 1,
      squashStretchY: 1,
      eyeExpression: 'happy',
      facingRight: true
    };

    this.p2State = {
      x: 90,
      y: 195,
      vx: 0,
      vy: 0,
      isGrounded: true,
      isJumping: false,
      runCycle: 0,
      squashStretchX: 1,
      squashStretchY: 1,
      eyeExpression: 'happy',
      facingRight: true
    };

    this.petState = {
      x: 75,
      y: 110,
      vx: 0,
      vy: 0,
      isGrounded: true,
      isJumping: false,
      runCycle: 0,
      squashStretchX: 1,
      squashStretchY: 1,
      eyeExpression: 'happy',
      facingRight: true
    };

    this.container = document.createElement('div');
    this.container.id = 'coop-select-overlay-root';
    this.container.className = 'coop-select-overlay hidden';
    document.body.appendChild(this.container);

    this.render();
  }

  public open(): void {
    this.isOpen = true;
    this.container.classList.remove('hidden');
    this.render();
    this.startPreviewLoop();
  }

  public close(): void {
    this.isOpen = false;
    this.container.classList.add('hidden');
    this.stopPreviewLoop();
  }

  public isVisible(): boolean {
    return this.isOpen;
  }

  public setConfig(newConfig: GameConfig): void {
    this.config = JSON.parse(JSON.stringify(newConfig));
    if (this.isOpen) this.render();
  }

  private startPreviewLoop(): void {
    if (this.previewAnimFrame !== null) return;

    let lastT = performance.now();
    const tick = (now: number) => {
      if (!this.isOpen) return;
      const dt = Math.min((now - lastT) / 1000, 0.05);
      lastT = now;
      this.animTime += dt;

      // Update idle breathing
      this.p1State.runCycle = this.animTime * 3.5;
      this.p2State.runCycle = this.animTime * 3.5 + 0.4;
      this.petState.runCycle = this.animTime * 4.0;

      // Render P1 Canvas
      if (this.p1Canvas) {
        const ctx = this.p1Canvas.getContext('2d');
        if (ctx) {
          ctx.clearRect(0, 0, this.p1Canvas.width, this.p1Canvas.height);
          CharacterRenderer.renderMom(ctx, this.config.parents.mom, this.p1State, 1.35);
        }
      }

      // Render P2 Canvas
      if (this.p2Canvas) {
        const ctx = this.p2Canvas.getContext('2d');
        if (ctx) {
          ctx.clearRect(0, 0, this.p2Canvas.width, this.p2Canvas.height);
          CharacterRenderer.renderDad(ctx, this.config.parents.dad, this.p2State, 1.35);
        }
      }

      // Render Pet Canvas
      if (this.petCanvas && this.config.pet.enabled) {
        const ctx = this.petCanvas.getContext('2d');
        if (ctx) {
          ctx.clearRect(0, 0, this.petCanvas.width, this.petCanvas.height);
          CharacterRenderer.renderPet(ctx, this.config.pet, this.petState, 1.4);
        }
      }

      this.previewAnimFrame = requestAnimationFrame(tick);
    };

    this.previewAnimFrame = requestAnimationFrame(tick);
  }

  private stopPreviewLoop(): void {
    if (this.previewAnimFrame !== null) {
      cancelAnimationFrame(this.previewAnimFrame);
      this.previewAnimFrame = null;
    }
  }

  private render(): void {
    const isPt = this.config.language === 'pt';
    const t = getTranslations(this.config.language);

    const momPalettes: Record<OutfitTheme, Array<{ name: string; color: string }>> = {
      casual: [
        { name: 'Rosa Festa', color: '#ec4899' },
        { name: 'Roxo Lavanda', color: '#a855f7' },
        { name: 'Ciano Neon', color: '#06b6d4' },
        { name: 'Dourado Suave', color: '#eab308' },
        { name: 'Rubi Chic', color: '#ef4444' }
      ],
      cyberpunk: [
        { name: 'Neon Samurai', color: '#facc15' },
        { name: 'Night City Ciano', color: '#00d2ff' },
        { name: 'Cyber Magenta', color: '#ff2a85' },
        { name: 'Netrunner Roxo', color: '#9333ea' },
        { name: 'Stealth Black', color: '#0f172a' }
      ],
      dragon_knight: [
        { name: 'Escamas de Dragão Vermelho', color: '#dc2626' },
        { name: 'Ouro Dracônico Real', color: '#d97706' },
        { name: 'Aço Valiriano Escuro', color: '#334155' },
        { name: 'Dragão Esmeralda', color: '#059669' },
        { name: 'Safira Mística', color: '#2563eb' }
      ]
    };

    const dadPalettes: Record<OutfitTheme, Array<{ name: string; color: string }>> = {
      casual: [
        { name: 'Azul Arcade', color: '#3b82f6' },
        { name: 'Verde Moderno', color: '#10b981' },
        { name: 'Preto Grafite', color: '#1e293b' },
        { name: 'Laranja Urbano', color: '#f97316' },
        { name: 'Bordeaux Elegante', color: '#881337' }
      ],
      cyberpunk: [
        { name: 'Neon Samurai Yellow', color: '#facc15' },
        { name: 'Ciano Cyber-Optic', color: '#00d2ff' },
        { name: 'Magenta Synthwave', color: '#ff2a85' },
        { name: 'Aço Cromado', color: '#0284c7' },
        { name: 'Sombra de Night City', color: '#020617' }
      ],
      dragon_knight: [
        { name: 'Armadura Dragão de Fogo', color: '#b91c1c' },
        { name: 'Placa de Aço e Ouro', color: '#475569' },
        { name: 'Dragão de Bronze', color: '#b45309' },
        { name: 'Manto do Dragão Negro', color: '#0f172a' },
        { name: 'Cavaleiro Esmeralda', color: '#065f46' }
      ]
    };

    const petColors = [
      { name: 'Dourado/Caramelo', primary: '#eab308', secondary: '#fef08a' },
      { name: 'Branco Neve', primary: '#f8fafc', secondary: '#e2e8f0' },
      { name: 'Preto Noite', primary: '#1e293b', secondary: '#475569' },
      { name: 'Chocolate', primary: '#78350f', secondary: '#b45309' }
    ];

    const currentMomPalette = momPalettes[this.config.parents.mom.outfitTheme] || momPalettes.casual;
    const currentDadPalette = dadPalettes[this.config.parents.dad.outfitTheme] || dadPalettes.casual;

    this.container.innerHTML = `
      <div class="coop-select-modal">
        <!-- HEADER -->
        <div class="coop-modal-header">
          <div class="coop-badge-title">★ CHOOSE YOUR SQUAD ★</div>
          <h1>${isPt ? 'SELEÇÃO CO-OP DE PERSONAGENS' : 'CO-OP CHARACTER SELECTION'}</h1>
          <p>${isPt ? 'Personalize o visual da Mamãe e do Papai separadamente antes da aventura!' : 'Customize Mom and Dad outfits separately before the mission!'}</p>
        </div>

        <!-- MAIN PODIUMS GRID (P1 vs P2) -->
        <div class="coop-podiums-container">
          
          <!-- PODIUM 1: PLAYER 1 (MAMÃE) -->
          <div class="player-podium p1-podium ${this.p1Ready ? 'ready' : ''}">
            <div class="podium-header p1-header">
              <span class="player-tag">PLAYER 1</span>
              <h2>👩 ${this.config.parents.mom.name}</h2>
            </div>

            <div class="podium-canvas-box">
              <canvas id="coop-p1-canvas" width="180" height="220"></canvas>
            </div>

            <!-- Theme Tabs -->
            <div class="podium-section">
              <label class="section-label">${t.outfitTheme}:</label>
              <div class="theme-pill-tabs">
                <button type="button" class="pill-tab ${this.config.parents.mom.outfitTheme === 'casual' ? 'active' : ''}" data-p1-theme="casual">
                  👕 Casual
                </button>
                <button type="button" class="pill-tab ${this.config.parents.mom.outfitTheme === 'cyberpunk' ? 'active' : ''}" data-p1-theme="cyberpunk">
                  ⚡ Cyberpunk
                </button>
                <button type="button" class="pill-tab ${this.config.parents.mom.outfitTheme === 'dragon_knight' ? 'active' : ''}" data-p1-theme="dragon_knight">
                  🐉 Dragão
                </button>
              </div>
            </div>

            <!-- Color Swatches -->
            <div class="podium-section">
              <label class="section-label">${t.outfit}:</label>
              <div class="swatches-grid">
                ${currentMomPalette.map(o => `
                  <button type="button" class="podium-swatch ${this.config.parents.mom.outfitColor === o.color ? 'active' : ''}"
                    style="background-color: ${o.color};" data-p1-color="${o.color}" title="${o.name}">
                  </button>
                `).join('')}
              </div>
            </div>

            <!-- Hairstyle -->
            <div class="podium-section">
              <label class="section-label">${t.hair}:</label>
              <select id="p1-sel-hair" class="podium-select">
                <option value="long" ${this.config.parents.mom.hairStyle === 'long' ? 'selected' : ''}>${isPt ? 'Longo Fluido' : 'Long Flowing'}</option>
                <option value="bun" ${this.config.parents.mom.hairStyle === 'bun' ? 'selected' : ''}>${isPt ? 'Coque Alto' : 'Top Bun'}</option>
                <option value="curly" ${this.config.parents.mom.hairStyle === 'curly' ? 'selected' : ''}>${isPt ? 'Cacheado' : 'Curly'}</option>
                <option value="short" ${this.config.parents.mom.hairStyle === 'short' ? 'selected' : ''}>${isPt ? 'Curto Moderno' : 'Short Modern'}</option>
              </select>
            </div>

            <!-- Ready Button -->
            <button type="button" class="podium-ready-btn p1-btn ${this.p1Ready ? 'active' : ''}" id="btn-p1-ready">
              ${this.p1Ready ? '✔ READY! / PRONTA!' : 'PRONTA? CLIQUE AQUI!'}
            </button>
          </div>

          <!-- PODIUM 2: PLAYER 2 (PAPAI) -->
          <div class="player-podium p2-podium ${this.p2Ready ? 'ready' : ''}">
            <div class="podium-header p2-header">
              <span class="player-tag">PLAYER 2</span>
              <h2>👨 ${this.config.parents.dad.name}</h2>
            </div>

            <div class="podium-canvas-box">
              <canvas id="coop-p2-canvas" width="180" height="220"></canvas>
            </div>

            <!-- Theme Tabs -->
            <div class="podium-section">
              <label class="section-label">${t.outfitTheme}:</label>
              <div class="theme-pill-tabs">
                <button type="button" class="pill-tab ${this.config.parents.dad.outfitTheme === 'casual' ? 'active' : ''}" data-p2-theme="casual">
                  👕 Casual
                </button>
                <button type="button" class="pill-tab ${this.config.parents.dad.outfitTheme === 'cyberpunk' ? 'active' : ''}" data-p2-theme="cyberpunk">
                  ⚡ Cyberpunk
                </button>
                <button type="button" class="pill-tab ${this.config.parents.dad.outfitTheme === 'dragon_knight' ? 'active' : ''}" data-p2-theme="dragon_knight">
                  🐉 Dragão
                </button>
              </div>
            </div>

            <!-- Color Swatches -->
            <div class="podium-section">
              <label class="section-label">${t.outfit}:</label>
              <div class="swatches-grid">
                ${currentDadPalette.map(o => `
                  <button type="button" class="podium-swatch ${this.config.parents.dad.outfitColor === o.color ? 'active' : ''}"
                    style="background-color: ${o.color};" data-p2-color="${o.color}" title="${o.name}">
                  </button>
                `).join('')}
              </div>
            </div>

            <!-- Hairstyle & Accessories -->
            <div class="podium-section">
              <label class="section-label">${t.hair}:</label>
              <select id="p2-sel-hair" class="podium-select">
                <option value="short" ${this.config.parents.dad.hairStyle === 'short' ? 'selected' : ''}>${isPt ? 'Curto Clássico' : 'Short Classic'}</option>
                <option value="fade" ${this.config.parents.dad.hairStyle === 'fade' ? 'selected' : ''}>Fade</option>
                <option value="curly" ${this.config.parents.dad.hairStyle === 'curly' ? 'selected' : ''}>${isPt ? 'Cacheado' : 'Curly'}</option>
                <option value="bald" ${this.config.parents.dad.hairStyle === 'bald' ? 'selected' : ''}>${isPt ? 'Careca' : 'Bald'}</option>
              </select>

              <div class="accessories-row">
                <label class="acc-pill">
                  <input type="checkbox" id="p2-chk-beard" ${this.config.parents.dad.hasBeard ? 'checked' : ''} />
                  🧔 ${t.beard}
                </label>
                <label class="acc-pill">
                  <input type="checkbox" id="p2-chk-glasses" ${this.config.parents.dad.hasGlasses ? 'checked' : ''} />
                  👓 ${t.glasses}
                </label>
              </div>
            </div>

            <!-- Ready Button -->
            <button type="button" class="podium-ready-btn p2-btn ${this.p2Ready ? 'active' : ''}" id="btn-p2-ready">
              ${this.p2Ready ? '✔ READY! / PRONTO!' : 'PRONTO? CLIQUE AQUI!'}
            </button>
          </div>

        </div>

        <!-- PET COMPANION TRAY (MIDDLE/BOTTOM) -->
        <div class="pet-companion-tray">
          <div class="pet-tray-left">
            <canvas id="coop-pet-canvas" width="150" height="120"></canvas>
          </div>
          <div class="pet-tray-right">
            <div class="pet-title-row">
              <h3>🐾 ${t.petSkin}: ${this.config.pet.name}</h3>
              <div class="pet-species-toggles">
                <button type="button" class="pet-btn ${this.config.pet.enabled && this.config.pet.type === 'dog' ? 'active' : ''}" data-pet-type="dog">🐶 Dog</button>
                <button type="button" class="pet-btn ${this.config.pet.enabled && this.config.pet.type === 'cat' ? 'active' : ''}" data-pet-type="cat">🐱 Cat</button>
                <button type="button" class="pet-btn ${!this.config.pet.enabled || this.config.pet.type === 'none' ? 'active' : ''}" data-pet-type="none">🚫 Off</button>
              </div>
            </div>
            
            <div class="pet-options-inline">
              <!-- Pet Outfit Theme -->
              <div class="theme-pill-tabs mini">
                <button type="button" class="pill-tab ${this.config.pet.outfitTheme === 'casual' ? 'active' : ''}" data-pet-theme="casual">👕 Bandana</button>
                <button type="button" class="pill-tab ${this.config.pet.outfitTheme === 'cyberpunk' ? 'active' : ''}" data-pet-theme="cyberpunk">⚡ Cyber-Visor</button>
                <button type="button" class="pill-tab ${this.config.pet.outfitTheme === 'dragon_knight' ? 'active' : ''}" data-pet-theme="dragon_knight">🐉 Asas Dragão</button>
              </div>

              <!-- Pet Colors -->
              <div class="swatches-grid mini">
                ${petColors.map(c => `
                  <button type="button" class="podium-swatch mini ${this.config.pet.primaryColor === c.primary ? 'active' : ''}"
                    style="background-color: ${c.primary};" data-pet-color-primary="${c.primary}" data-pet-color-secondary="${c.secondary}" title="${c.name}">
                  </button>
                `).join('')}
              </div>
            </div>
          </div>
        </div>

        <!-- FOOTER & LAUNCH BUTTON -->
        <div class="coop-footer">
          <button type="button" class="launch-coop-btn" id="btn-launch-coop">
            ▶ ${isPt ? 'INICIAR ANIMAÇÃO DO CARRINHO 🚀' : 'START CART ANIMATION 🚀'}
          </button>
        </div>
      </div>
    `;

    this.bindEvents();
  }

  private bindEvents(): void {
    this.p1Canvas = this.container.querySelector('#coop-p1-canvas') as HTMLCanvasElement;
    this.p2Canvas = this.container.querySelector('#coop-p2-canvas') as HTMLCanvasElement;
    this.petCanvas = this.container.querySelector('#coop-pet-canvas') as HTMLCanvasElement;

    // P1 Theme Tabs
    this.container.querySelectorAll('[data-p1-theme]').forEach((btn) => {
      btn.addEventListener('click', (e) => {
        e.stopPropagation();
        const theme = (btn as HTMLElement).dataset.p1Theme as OutfitTheme;
        if (theme) {
          this.config.parents.mom.outfitTheme = theme;
          if (theme === 'cyberpunk') this.config.parents.mom.outfitColor = '#facc15';
          else if (theme === 'dragon_knight') this.config.parents.mom.outfitColor = '#dc2626';
          else this.config.parents.mom.outfitColor = '#ec4899';
          this.notifyChange();
          this.render();
        }
      });
    });

    // P1 Color Swatches
    this.container.querySelectorAll('[data-p1-color]').forEach((btn) => {
      btn.addEventListener('click', (e) => {
        e.stopPropagation();
        const color = (btn as HTMLElement).dataset.p1Color;
        if (color) {
          this.config.parents.mom.outfitColor = color;
          this.notifyChange();
          this.render();
        }
      });
    });

    // P1 Hair
    const p1HairSel = this.container.querySelector('#p1-sel-hair') as HTMLSelectElement | null;
    p1HairSel?.addEventListener('change', () => {
      this.config.parents.mom.hairStyle = p1HairSel.value as HairStyleMom;
      this.notifyChange();
    });

    // P1 Ready Toggle
    this.container.querySelector('#btn-p1-ready')?.addEventListener('click', (e) => {
      e.stopPropagation();
      this.p1Ready = !this.p1Ready;
      this.render();
    });

    // P2 Theme Tabs
    this.container.querySelectorAll('[data-p2-theme]').forEach((btn) => {
      btn.addEventListener('click', (e) => {
        e.stopPropagation();
        const theme = (btn as HTMLElement).dataset.p2Theme as OutfitTheme;
        if (theme) {
          this.config.parents.dad.outfitTheme = theme;
          if (theme === 'cyberpunk') this.config.parents.dad.outfitColor = '#00d2ff';
          else if (theme === 'dragon_knight') this.config.parents.dad.outfitColor = '#475569';
          else this.config.parents.dad.outfitColor = '#3b82f6';
          this.notifyChange();
          this.render();
        }
      });
    });

    // P2 Color Swatches
    this.container.querySelectorAll('[data-p2-color]').forEach((btn) => {
      btn.addEventListener('click', (e) => {
        e.stopPropagation();
        const color = (btn as HTMLElement).dataset.p2Color;
        if (color) {
          this.config.parents.dad.outfitColor = color;
          this.notifyChange();
          this.render();
        }
      });
    });

    // P2 Hair
    const p2HairSel = this.container.querySelector('#p2-sel-hair') as HTMLSelectElement | null;
    p2HairSel?.addEventListener('change', () => {
      this.config.parents.dad.hairStyle = p2HairSel.value as HairStyleDad;
      this.notifyChange();
    });

    // P2 Beard & Glasses
    const p2BeardChk = this.container.querySelector('#p2-chk-beard') as HTMLInputElement | null;
    p2BeardChk?.addEventListener('change', () => {
      this.config.parents.dad.hasBeard = p2BeardChk.checked;
      this.notifyChange();
    });

    const p2GlassesChk = this.container.querySelector('#p2-chk-glasses') as HTMLInputElement | null;
    p2GlassesChk?.addEventListener('change', () => {
      this.config.parents.dad.hasGlasses = p2GlassesChk.checked;
      this.notifyChange();
    });

    // P2 Ready Toggle
    this.container.querySelector('#btn-p2-ready')?.addEventListener('click', (e) => {
      e.stopPropagation();
      this.p2Ready = !this.p2Ready;
      this.render();
    });

    // Pet Species
    this.container.querySelectorAll('[data-pet-type]').forEach((btn) => {
      btn.addEventListener('click', (e) => {
        e.stopPropagation();
        const pType = (btn as HTMLElement).dataset.petType as PetType;
        if (pType === 'none') {
          this.config.pet.enabled = false;
          this.config.pet.type = 'none';
        } else {
          this.config.pet.enabled = true;
          this.config.pet.type = pType;
        }
        this.notifyChange();
        this.render();
      });
    });

    // Pet Theme
    this.container.querySelectorAll('[data-pet-theme]').forEach((btn) => {
      btn.addEventListener('click', (e) => {
        e.stopPropagation();
        const theme = (btn as HTMLElement).dataset.petTheme as OutfitTheme;
        if (theme) {
          this.config.pet.outfitTheme = theme;
          this.notifyChange();
          this.render();
        }
      });
    });

    // Pet Colors
    this.container.querySelectorAll('[data-pet-color-primary]').forEach((btn) => {
      btn.addEventListener('click', (e) => {
        e.stopPropagation();
        const prim = (btn as HTMLElement).dataset.petColorPrimary;
        const sec = (btn as HTMLElement).dataset.petColorSecondary;
        if (prim && sec) {
          this.config.pet.primaryColor = prim;
          this.config.pet.secondaryColor = sec;
          this.notifyChange();
          this.render();
        }
      });
    });

    // Launch Co-op Mission Button
    this.container.querySelector('#btn-launch-coop')?.addEventListener('click', (e) => {
      e.stopPropagation();
      this.close();
      this.onStartGame();
    });
  }

  private notifyChange(): void {
    saveGameConfig(this.config);
    this.onConfigChanged(this.config);
  }
}
