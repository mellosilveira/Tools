import { Language, Gender } from './gameConfig';

export interface Translations {
  titlePrefix: string;
  titleMain: string;
  subtitle: string;
  pressStart: string;
  customizeSkinsBtn: string;
  chooseSkinsTitle: string;
  momSkin: string;
  dadSkin: string;
  petSkin: string;
  outfit: string;
  hair: string;
  beard: string;
  glasses: string;
  petType: string;
  petColor: string;
  outfitTheme: string;
  themeCasual: string;
  themeCyberpunk: string;
  themeDragon: string;
  startRun: string;
  player1Ready: string;
  player2Ready: string;
  petJoinedTitle: string;
  petJoinedDesc: string;
  clickToContinue: string;
  comboStyle: string;
  comboLove: string;
  comboHype: string;
  teamBoy: string;
  teamGirl: string;
  loveMeter: string;
  phaseTitle: string;
  trollBlockTitle: string;
  trollBlockDuck: string;
  chargingBoy: string;
  glitchOverride: string;
  chargingGirl: string;
  tieAlert: string;
  tieDesc: string;
  acceptChallenge: string;
  challengeJumpTitle: string;
  challengeJumpDesc: string;
  jumpButton: string;
  perfectJump: string;
  autoAssistTriggered: string;
  challengeShowerTitle: string;
  challengeShowerDesc: string;
  tapRapidly: string;
  maxPower: string;
  finalBlockTitle: string;
  finalBlockDesc: string;
  hitCount: string;
  finalHitPrompt: string;
  boyBanner: string;
  girlBanner: string;
  revealSubtitle: string;
  statsLove: string;
  statsSquad: string;
  replayButton: string;
  fireworksButton: string;
  modalTitle: string;
  adminHint: string;
}

const TRANSLATIONS: Record<Language, Translations> = {
  pt: {
    titlePrefix: 'CHÁ REVELAÇÃO ARCADE',
    titleMain: 'SUPER BEBÊ RUNNER 2D',
    subtitle: 'UMA AVENTURA CO-OP ÉPICA DE AMOR',
    pressStart: 'CLIQUE OU APERTE ESPAÇO PARA INICIAR',
    customizeSkinsBtn: '🎨 ESCOLHER SKINS / VISUAL',
    chooseSkinsTitle: 'ESCOLHA O VISUAL DA FAMÍLIA',
    momSkin: 'Mamãe',
    dadSkin: 'Papai',
    petSkin: 'Pet da Família',
    outfit: 'Roupa',
    hair: 'Cabelo',
    beard: 'Barba',
    glasses: 'Óculos',
    petType: 'Espécie',
    petColor: 'Cor do Pêlo',
    outfitTheme: 'Estilo do Traje',
    themeCasual: '👕 Festa Casual',
    themeCyberpunk: '⚡ Cyberpunk 2077',
    themeDragon: '🐉 RPG Dragão',
    startRun: 'PRONTO! INICIAR CORRIDA 🚀',
    player1Ready: 'P1: {name}',
    player2Ready: 'P2: {name}',
    petJoinedTitle: 'UM NOVO AMIGO ENTROU NO TIME!',
    petJoinedDesc: '{petName} chegou correndo para acelerar a festa!',
    clickToContinue: 'CLIQUE OU APERTE ESPAÇO PARA CONTINUAR',
    comboStyle: '+100 ESTILO!',
    comboLove: '+500 AMOR!',
    comboHype: 'SUPER COMBO!',
    teamBoy: 'TIME MENINO',
    teamGirl: 'TIME MENINA',
    loveMeter: 'ENERGIA DO AMOR',
    phaseTitle: 'FASE',
    trollBlockTitle: 'OPA! BLOCO TROLL!',
    trollBlockDuck: 'QUACK! O BLOCO REAL AINDA ESTÁ POR VIR!',
    chargingBoy: 'CARREGANDO TIME MENINO... 99%',
    glitchOverride: 'ALERTA: SOBRECARGA DE SISTEMA!',
    chargingGirl: 'CARREGANDO TIME MENINA... 99%',
    tieAlert: 'EMPATE TÉCNICO DETECTADO!',
    tieDesc: 'O sistema sobrecarregou! Vença os Desafios Finais para destravar o Player 3!',
    acceptChallenge: 'ACEITAR OS DESAFIOS!',
    challengeJumpTitle: 'DESAFIO 1: SALTO DAS NUVENS!',
    challengeJumpDesc: 'Pule no tempo certo para desviar dos Palpites e Noites em Claro!',
    jumpButton: 'PULAR! [ESPAÇO / CLIQUE]',
    perfectJump: 'SALTO PERFEITO! +300%',
    autoAssistTriggered: 'ASSISTÊNCIA ATIVADA!',
    challengeShowerTitle: 'DESAFIO 2: CHUVA DE CARINHO!',
    challengeShowerDesc: 'Clique ou aperte espaço rápido para coletar itens e encher a Barra de Amor!',
    tapRapidly: 'CLIQUE RÁPIDO!',
    maxPower: 'PODER MÁXIMO 100% ATINGIDO!',
    finalBlockTitle: 'O MEGA BLOCO MISTERIOSO (?)',
    finalBlockDesc: 'Golpeie o bloco com toda a energia para liberar a revelação!',
    hitCount: 'GOLPE {current} DE {total}!',
    finalHitPrompt: 'GOLPE FINAL! DETONAR!',
    boyBanner: "NOVO PLAYER DESBLOQUEADO: É MENINO!",
    girlBanner: "NOVO PLAYER DESBLOQUEADO: É MENINA!",
    revealSubtitle: 'A MAIOR AVENTURA DA VIDA COMEÇA AGORA!',
    statsLove: 'NÍVEL DE AMOR: 9999%',
    statsSquad: 'SQUAD COMPLETO: MAMÃE + PAPAI + PET + PLAYER 3!',
    replayButton: 'JOGAR NOVAMENTE ↺',
    fireworksButton: 'MAIS FOGOS! 🎆',
    modalTitle: 'PAINEL SECRETO DE CONFIGURAÇÃO',
    adminHint: 'Dica: Pressione "C" ou clique na engrenagem para abrir este painel.'
  },
  en: {
    titlePrefix: 'ARCADE GENDER REVEAL',
    titleMain: 'SUPER BABY RUNNER 2D',
    subtitle: 'AN EPIC CO-OP RUN OF LOVE',
    pressStart: 'CLICK OR PRESS SPACE TO START',
    customizeSkinsBtn: '🎨 CHOOSE SKINS / OUTFITS',
    chooseSkinsTitle: 'CUSTOMIZE FAMILY SQUAD',
    momSkin: 'Mom',
    dadSkin: 'Dad',
    petSkin: 'Family Pet',
    outfit: 'Outfit',
    hair: 'Hair',
    beard: 'Beard',
    glasses: 'Glasses',
    petType: 'Type',
    petColor: 'Fur Color',
    outfitTheme: 'Outfit Style',
    themeCasual: '👕 Casual Party',
    themeCyberpunk: '⚡ Cyberpunk 2077',
    themeDragon: '🐉 Dragon RPG',
    startRun: 'READY! START RUN 🚀',
    player1Ready: 'P1: {name}',
    player2Ready: 'P2: {name}',
    petJoinedTitle: 'A NEW FRIEND JOINED THE SQUAD!',
    petJoinedDesc: '{petName} dashed in to boost the excitement!',
    clickToContinue: 'CLICK OR PRESS SPACE TO CONTINUE',
    comboStyle: '+100 STYLE!',
    comboLove: '+500 LOVE!',
    comboHype: 'SUPER COMBO!',
    teamBoy: 'TEAM BOY',
    teamGirl: 'TEAM GIRL',
    loveMeter: 'LOVE & HYPE METER',
    phaseTitle: 'STAGE',
    trollBlockTitle: 'WOAH! TROLL BLOCK!',
    trollBlockDuck: 'QUACK! THE REAL MYSTERY BLOCK IS AHEAD!',
    chargingBoy: 'CHARGING TEAM BOY... 99%',
    glitchOverride: 'WARNING: SYSTEM COUNTER-OVERRIDE!',
    chargingGirl: 'CHARGING TEAM GIRL... 99%',
    tieAlert: 'TECHNICAL TIE DETECTED!',
    tieDesc: 'System overload! Complete the Final Challenges to unlock Player 3!',
    acceptChallenge: 'ACCEPT CHALLENGES!',
    challengeJumpTitle: 'CHALLENGE 1: CLOUD JUMP!',
    challengeJumpDesc: 'Jump with perfect timing to dodge Guessing and Sleepless Nights!',
    jumpButton: 'JUMP! [SPACE / CLICK]',
    perfectJump: 'PERFECT JUMP! +300%',
    autoAssistTriggered: 'AUTO-ASSIST ACTIVATED!',
    challengeShowerTitle: 'CHALLENGE 2: BABY ITEM FEVER!',
    challengeShowerDesc: 'Click or mash space rapidly to collect items and fill the Love Bar!',
    tapRapidly: 'MASH QUICKLY!',
    maxPower: 'MAXIMUM 100% POWER REACHED!',
    finalBlockTitle: 'THE MEGA MYSTERY BLOCK (?)',
    finalBlockDesc: 'Strike the block with all your power to trigger the reveal!',
    hitCount: 'STRIKE {current} OF {total}!',
    finalHitPrompt: 'FINAL STRIKE! DETONATE!',
    boyBanner: "NEW PLAYER UNLOCKED: IT'S A BOY!",
    girlBanner: "NEW PLAYER UNLOCKED: IT'S A GIRL!",
    revealSubtitle: 'THE GREATEST ADVENTURE OF LIFE BEGINS NOW!',
    statsLove: 'LOVE LEVEL: 9999%',
    statsSquad: 'FULL SQUAD: MOM + DAD + PET + PLAYER 3!',
    replayButton: 'PLAY AGAIN ↺',
    fireworksButton: 'MORE FIREWORKS! 🎆',
    modalTitle: 'SECRET CUSTOMIZER PANEL',
    adminHint: 'Tip: Press "C" or click the gear icon to open this panel.'
  }
};

export function getTranslations(lang: Language): Translations {
  return TRANSLATIONS[lang] || TRANSLATIONS.pt;
}

export function formatText(template: string, vars: Record<string, string | number>): string {
  let result = template;
  for (const [key, val] of Object.entries(vars)) {
    result = result.replace(new RegExp(`\\{${key}\\}`, 'g'), String(val));
  }
  return result;
}

export function getRevealBannerText(gender: Gender, lang: Language): string {
  const t = getTranslations(lang);
  return gender === 'boy' ? t.boyBanner : t.girlBanner;
}
