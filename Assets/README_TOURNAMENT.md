# 🏆 Control de Torneig Janken - Challonge Bracket per a Unity (UI Toolkit)

Aquest és un sistema complet de gestió de quadre de torneig (*bracket manager*) en format de **Single Elimination** (estil Challonge) creat exclusivament amb **Unity UI Toolkit** (`UXML`, `USS`, `C#`).

---

## 📁 Estructura dels Fitxers Creats

```
Assets/
├── UI/
│   ├── TournamentManager.uxml    # Disseny de la interfície d'usuari
│   └── TournamentManager.uss     # Estils visuals Cyberpunk Dark Mode
└── Scripts/
    ├── Models/
    │   ├── Player.cs             # Model de dades de Jugador
    │   ├── Match.cs              # Model de dades de Partida / Ronda
    │   └── TournamentModel.cs    # Lògica del Torneig (Bracket Generator, Avance de guanyadors, BYEs)
    └── Controllers/
        └── TournamentUIController.cs # Controlador MonoBehaviour connectat a UIDocument
```

---

## 🚀 Com activar-ho a l'Editor de Unity

1. Obrir el projecte a **Unity**.
2. Anar a la llista d'escenes (`Assets/Scenes/SampleScene.unity`).
3. Crear un nou **GameObject** a la jerarquia de l'escena i anomenar-lo `TournamentUI`.
4. Afegir el component **`UIDocument`** a aquest GameObject:
   - Camp **Visual Tree Asset**: Assignar `Assets/UI/TournamentManager.uxml`.
   - Camp **Panel Settings**: Assignar el `PanelSettings` predeterminat del projecte o crear-ne un de nou (amb suport per a UI Toolkit).
5. Afegir el component **`TournamentUIController`** al mateix GameObject.
6. Donar al **Play** a Unity!

---

## ✨ Funcionalitats Destacades

- **Configuració de Jugadors**:
  - Afegir jugadors manualment o triar presets ràpids (4, 8 o 16 jugadors).
  - Esborrar o barrejar (*shuffle*) llavors (*seeds*) en qualsevol moment.
- **Quadre Dinàmic Estil Challonge**:
  - Generació automàtica de rondes (Quarts de Final, Semifinals, Final).
  - Suport per a nombres impars de jugadors amb passis automàtics (*BYEs*).
- **Fàcil Declaració de Guanyadors**:
  - Clic directe a la targeta o botó "Marcar Guanyador" de qualsevol slot de partida.
  - Avance automàtic del guanyador a la següent ronda.
  - Visualització del podi del **Campió del Torneig** al finalitzar l'última partida.
