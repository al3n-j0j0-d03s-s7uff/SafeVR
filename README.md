# SafeVR 🦺🥽

> **Interactive VR & Desktop Emergency Response & Safety Training Simulation**  
> Built with **Unity 6 (6000.4.10f1)** and the **Universal Render Pipeline (URP)** in C#.

[![Unity Version](https://img.shields.io/badge/Unity-6000.4.10f1-blue.svg?logo=unity)](https://unity.com/)
[![Render Pipeline](https://img.shields.io/badge/Pipeline-URP%2017.4.0-lightgrey.svg)](https://unity.com/srp/Universal-Render-Pipeline)
[![Language](https://img.shields.io/badge/C%23-10.0%2B-blue.svg?logo=csharp)](https://docs.microsoft.com/dotnet/csharp/)
[![Platform](https://img.shields.io/badge/Platform-Desktop%20%7C%20VR%20Ready-green.svg)]()

---

## 📌 Overview

**SafeVR** is an immersive safety simulation designed to bridge theoretical workplace safety protocols with hands-on practice in a risk-free virtual environment. 

The project features a full training and assessment loop:
1. **Interactive Training Hall**: Practice identifying hazards, isolating emergency electrical systems, and inspecting firefighting apparatus.
2. **Dynamic Inspection Modals**: Learn the **P.A.S.S.** method (Pull, Aim, Squeeze, Sweep) and emergency cutoff protocols through contextual interactable popups.
3. **Knowledge Assessment**: Complete a structured evaluation covering hazard isolation and fire classification (Pass threshold: 66%).

---

## 🚀 Key Features

* **Decoupled VR / Desktop Architecture:**
  * Uses an input-agnostic raycast interactor (`Interactor` & `IInteractable`).
  * Features a fully functional desktop FPS controller (`DesktopFPSController`) for instant development and testing without a headset, ready to drop in an **XR Origin** for Meta Quest / OpenXR.
* **Procedural Scene Generation:**
  * Custom Unity Editor tools automatically generate materials, UI canvases, layout geometry, and scenes via code (`SafeVRSceneBuilder`).
* **Interactive Equipment & Hazards:**
  * **Class C Fire Extinguisher:** Inspection modal with P.A.S.S. protocol training.
  * **Emergency Cutoff Switch:** Isolates electrical machinery with audible and visual warning indicators.
  * **Emergency Evacuation Exit:** Teaches designated safe exit pathing.
* **Audio Synthesis:**
  * Procedurally synthesized runtime sound effects (`SoundEffectManager`)—no bulky external audio files required.
* **Real-time Assessment Engine:**
  * Randomized multiple-choice question bank with immediate rationale breakdown and scoring analytics.

---

## 🎮 Controls (Desktop Mode)

| Action | Key / Input |
|---|---|
| **Movement** | `W`, `A`, `S`, `D` |
| **Look Around** | `Mouse` |
| **Interact / Inspect** | `E` or `Left Mouse Button` |
| **Sprint** | `Left Shift` |
| **Jump** | `Space` |
| **Pause Menu** | `Tab` |
| **Unlock Cursor** | `Esc` |

---

## 📁 Project Architecture

```text
Assets/SafeVR/
├── Materials/        # Procedural URP Lit materials (Floor, Wall, Extinguisher, Hazard, etc.)
├── Scenes/           # Generated scene assets
│   ├── MainMenu.unity
│   ├── TrainingRoom.unity
│   └── Assessment.unity
├── Scripts/
│   ├── Core/         # SceneFlowManager, SoundEffectManager, TrainingProgress
│   ├── Player/       # DesktopFPSController, ReticleHUD
│   ├── Interaction/  # IInteractable, Interactor, EquipmentInteractable, SwitchInteractable
│   ├── Training/     # TrainingManager, TrainingObjective
│   ├── Assessment/   # AssessmentManager, AssessmentQuestion
│   ├── UI/           # MainMenuUI, TrainingRoomUI, AssessmentUI
│   └── Editor/       # SafeVRSceneBuilder, SafeVRMaterials, SafeVRUIBuilder
└── Settings/         # URP pipeline assets & Input System configurations
```

---

## 🛠️ Getting Started

### Prerequisites
* [Unity Hub](https://unity.com/download)
* **Unity Editor 6000.4.10f1** (Unity 6) with:
  * Universal Render Pipeline (URP)
  * Windows Build Support (or platform of choice)

### Installation
1. Clone the repository:
   ```bash
   git clone https://github.com/al3n-j0j0-d03s-s7uff/SafeVR.git
   ```
2. Open **Unity Hub** → Click **Add** → Select the cloned `SafeVR` folder.
3. Open the project in Unity 6.

### Running the Simulation
1. In the Project window, navigate to `Assets/SafeVR/Scenes/`.
2. Double-click **`MainMenu.unity`**.
3. Press **Play** (▶) in the Unity Editor.

*(Optional)* If you ever need to rebuild scenes from scratch, use the Unity top toolbar menu:  
`SafeVR` → `Build All Scenes (MainMenu + TrainingRoom + Assessment)`.

---

## 📜 License

This project is licensed under the MIT License - feel free to use and adapt it for educational and training purposes.
