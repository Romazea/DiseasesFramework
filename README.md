<h1 align="center">Diseases Framework</h1>

<p align="center">
  <strong>A high-performance, modular library for complex biological ecosystems in RimWorld.</strong><br>
  Developed by <b>Blaxer Studios</b>
</p>

<div align="center">
  <img src="https://img.shields.io/badge/RimWorld-1.6-green?style=for-the-badge&logo=rimworld" alt="RimWorld Version">
</div>

---

## 🧬 Overview

Welcome to the **Diseases Framework**! This library provides modders with high-performance, modular tools to implement complex disease transmission vectors. Our goal is to move beyond simple "random events" and create a truly interconnected biological ecosystem on the Rim.

### Key Features
* **Vector Diversity:** Combat, Ingestion, Zoonosis, Fomites, and Surgery-based infections.
* **High Performance:** Optimized with `TickRare` and hash interval checks to ensure zero TPS impact.
* **Developer Friendly:** Fully documented C# API and modular XML components.

---

## 🛠 Integrating the Framework

To use the shared behaviors and infection vectors of this framework in your own project, you must add it as a dependency. Insert the following block into your mod's `About.xml` file:

```xml
<modDependencies>
  <li>
    <packageId>BlaxerStudios.DiseasesFramework.Core</packageId>
    <displayName>Diseases Framework</displayName>
    <steamWorkshopUrl>https://steamcommunity.com/sharedfiles/filedetails/?id=3683664569</steamWorkshopUrl>
    <downloadUrl>https://github.com/Blaxer-Studios/DiseasesFramework</downloadUrl>
  </li>
</modDependencies>
```

It is also recommended that you add in Steam this framework mod to your list of Required Items.

