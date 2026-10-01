# Directori Vídeo NOU ÀRBITRE

Aquest directori està dedicat als recursos de vídeo per a la funció **NOU ÀRBITRE** del Torneig Janken.

## Requisits:
- Guardeu el vostre arxiu de vídeo (per exemple: `NOU_ARBITRE.mp4`, `nou_arbitre.mov`) dins d'aquesta carpeta (`Assets/Videos/NouArbitre/`).
- En obrir el projecte a Unity, els controladors (`TournamentUIController` i `Display2Controller`) detectaran i assignaran automàticament aquest VideoClip si no s'ha assignat manualment a l'Inspector.

## Controladors:
- **Botó GO**: Executa una transició *Stinger* (si està activa) i comença a reproduir el vídeo des del segon configurat a `goVideoStartTime`.
- **Botó REP**: Reprodueix el vídeo directament sense transició *Stinger* des del segon configurat a `repVideoStartTime`.
- **Àudio**: El vídeo es reprodueix amb l'àudio integrat a volum màxim (100%).
