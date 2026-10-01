# 🏆 Control de Torneig Janken - Sistema MultiDisplay & Challonge Bracket per a Unity (UI Toolkit)

Aquest és un sistema complet de gestió de quadre de torneig (*bracket manager*) en format de **Single Elimination** (estil Challonge) amb **suport MultiDisplay (Display 1 per a Control/Configuració i Display 2 per a Públic/Combat VS)** creat exclusivament amb **Unity UI Toolkit** (`UXML`, `USS`, `C#`).

---

## 📁 Estructura dels Fitxers del Projecte

```
Assets/
├── UI/
│   ├── TournamentManager.uxml    # Interfície principal de Control i Configuració (Display 1)
│   ├── TournamentManager.uss     # Estils visuals Girocòmic JANKEN per a Display 1
│   ├── Display2Manager.uxml      # Interfície de presentació per a Públic/Escena (Display 2)
│   └── Display2Manager.uss       # Estils visuals Girocòmic JANKEN per a Display 2 (Fons i UI Japonesa)
├── Sprites/
│   ├── Backgrounds/              # Fons i elements del cartell (Japanese_Background_Full.png, Japanese_Sun_Circle.png)
│   ├── UI/                       # Logotip, Banners i Icones de Pedra, Paper i Tisora (Icon_Rock, Icon_Paper, Icon_Scissors)
│   └── Posters/                  # Pàgines d'alta resolució exportades del cartell original de Girocòmic JANKEN
├── Videos/
│   └── NouArbitre/               # Carpeta dedicada als vídeos de NOU ÀRBITRE (ex: NOU_ARBITRE.mp4)
└── Scripts/
    ├── Models/
    │   ├── Player.cs             # Model de dades de Jugador
    │   ├── Match.cs              # Model de dades de Partida / Ronda
    │   └── TournamentModel.cs    # Lògica del Torneig, sincronització i estat MultiDisplay (`DisplayViewType`)
    └── Controllers/
        ├── TournamentUIController.cs # Controlador per al Display 1 (Control & Configuració)
        └── Display2Controller.cs     # Controlador per al Display 2 (Audiència & Pantalla VS & Vídeo)
```

---

## 🚀 Com configurar el MultiDisplay a Unity (1 Sol Clic!)

He creat una **Eina Automàtica d'Editor** perquè no hagis de configurar res manualment:

1. Obrir el projecte a **Unity**.
2. Anar al menú superior de Unity i fer clic a:
   **`Janken` ➔ `⚡ Configurar Escena MultiDisplay Automàticament`**
3. ¡I ja està! La tool crearà i configurarà automàticament els GameObjects `Display1_Control` i `Display2_Audience` a la jerarquia de l'escena amb els seus respectius `UIDocument` i `PanelSettings`.

---

## 🛠️ Configuració Manual (Opcional)

1. Obrir el projecte a **Unity**.
2. Anar a la jerarquia de l'escena (`Assets/Scenes/SampleScene.unity`).

### Configurar Display 1 (Control):
3. Crear un **GameObject** anomenat `Display1_Control`.
4. Afegir el component **`UIDocument`**:
   - **Visual Tree Asset**: `Assets/UI/TournamentManager.uxml`.
   - **Panel Settings**: Assignar el `PanelSettings` principal.
5. Afegir el component **`TournamentUIController`**.

### Configurar Display 2 (Pantalla de Públic / Combat):
6. Crear un segon **GameObject** anomenat `Display2_Audience`.
7. Afegir el component **`UIDocument`**:
   - **Visual Tree Asset**: `Assets/UI/Display2Manager.uxml`.
   - **Panel Settings**: Crear o assignar un `PanelSettings` amb **Target Display = Display 2** (o associat a una segona càmera).
8. Afegir el component **`Display2Controller`**.
9. Donar al **Play** a Unity!

---

## 🖥️ Funcionalitats MultiDisplay (Display 1 vs Display 2)

- **Display 1 (Control & Configuració)**:
  - Gestió completa de jugadors, rondes, marcatge de guanyadors i edició de noms.
  - **Barra de Control de Display 2**: botons `📊 Quadre`, `⚔️ Combat`, `🏆 Podi` per commutar a l'instant la vista enviada a la pantalla de públic.
  - **Botons Acció de Vídeo (`GO` i `REP`)**:
    - Ubicats fora dels arrays de navegació i amb un disseny més gran i vistós.
    - **▶ GO**: Executa la transició *Stinger* (si està activada) i inicia la reproducció del vídeo `NOU ARBITRE` des del timestamp configurat (`GO Start (s)`).
    - **🔄 REP**: Reprodueix el vídeo `NOU ARBITRE` directament sense transició *Stinger* des del timestamp configurat (`REP Start (s)`).
    - **Configuració de temps**: Es poden ajustar els segons d'inici en temps real a la barra lateral o des de l'Inspector de Unity.
    - **Àudio integrat**: El so del vídeo es reprodueix directament al 100% de volum.
  - **Botó ⚔️ als Combats**: clic ràpid en qualsevol enfrontament per enviar directament la pantalla VS d'aquell combat al Display 2.

- **Dreceres de Teclat (Hotkeys)**:
  - **`TAB`**: Posa la pantalla a **GO**. Si ja s'està a la vista de vídeo i es torna a prémer **`TAB`**, passa a **REP** (i manté **REP** en totes les premudes posteriors fins que se surti de la vista).
  - **`Page DOWN` / `↓`**: Obre sempre la vista del **Quadre**.
  - **`Page UP` / `↑`**: Obre sempre la vista de **Combat**.

- **Display 2 (Pantalla de Públic)**:
  - Totalment neta de botons, menús o camps d'edició.
  - **Vista Quadre / Classificació**: mostra el quadre complet actualitzat en temps real.
  - **Vista Combat VS (Goku vs Vegeta)**: pantalles d'enfrontament directe amb targetes de jugadors glowing, inicials, llavors, estat del combat i banners de victòria.
  - **Vista Vídeo NOU ÀRBITRE**: pantalla de reproducció de vídeo a pantalla completa amb canal d'àudio actiu.
  - **Arquitectura Extensible (`DisplayViewType`)**: dissenyada per afegir fàcilment noves vistes en el futur segons les teves necessitats.
