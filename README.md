#  Prototype Food ERP (v3.0 — Dual-Plane Owner Admin & Operational Suite)

**All-in-One Restaurant Operating System & Enterprise Resource Planning (ERP)** progettato secondo le **Apple Human Interface Guidelines (macOS Sequoia & iPadOS Pro)** e sviluppato dalla flotta multi-agente a 6 nodi (**Business Analyst, Planner, Coder, Verifier, Reviewer/Sec, Release & DevOps**).

---

## 🏗️ Architettura Dual-Plane (Separazione Ruoli)

1. **👑 Ambiente 1 — Owner & Admin Control ERP (`Plane 1`):**
   - Accessibile da **Sito Web Desktop**, **iPad Pro Executive** e **App Mobile Titolare**.
   - **Sidebar Enterprise a sinistra** con tutti i reparti gestionali: *Executive Control Tower, Magazzino & Lotti FEFO, Ricette & Distinta Base (BoM), Procurement Esterno B2B, Finanza Live P&L & Fatture XML SDI, HR Turni Brigata*.
2. **⚡ Ambiente 2 — App Operativa Sala, QR & Cucina KDS (`Plane 2`):**
   - Interfaccia di campo ad altissima velocità e zero distrazioni per **Camerieri (Palmarino)**, **Cucina (Monitor KDS Multi-Partita)** e **Clienti al Tavolo (QR Self-Ordering)**.
   - Nasconde i dati finanziari sensibili dell'azienda (EBITDA, costi fornitori, margini) sincronizzando ogni comanda o sfrido in tempo reale con l'ERP dell'Owner.

---

## 🚀 I 5 Market Gap Inclusi (Cosa manca alle altre App sul mercato)

1. **GAP #1 — Sentiment-to-Margin Matrix™:** Incrocia il margine finanziario del piatto (*Food Cost %*) con il sentiment NLP delle recensioni esterne (*Google, TheFork, TripAdvisor*), individuando le *"Trappole di Margine"* causate da cambi lotto/fornitore.
2. **GAP #2 — AI Multi-Vendor Price Broker + Meteo Forecast:** Confronta in tempo reale i listini B2B di più fornitori (*Longino, Metro, MARR*) e adatta le quantità di riordino alle prenotazioni e al meteo del giorno successivo.
3. **GAP #3 — Zero-Effort UoM Invoice Converter:** Converte automaticamente le unità di misura delle Fatture Elettroniche XML SDI (da *Casse/Colli* a *Grammi/ml* della ricetta) azzerando il data-entry manuale in cucina.
4. **GAP #4 — Warm AI Sommelier nel Menu QR:** Trasforma il menu QR da catalogo passivo a sommelier digitale che suggerisce il calice perfetto in base alle bottiglie realmente disponibili in cantina (`+22%` scontrino medio).
5. **GAP #5 — 1-Tap Hands-Free Kitchen Waste sul KDS:** Permette alla brigata di cucina di registrare sfridi o sostituzioni con un solo tocco durante il picco di servizio, aggiornando istantaneamente il *Food Cost Reale* nel Conto Economico dell'Owner.

---

## 📂 Struttura del Repository

- [`business_spec.md`](./business_spec.md) — Specifica funzionale completa, benchmark competitor (*Toast, Lightspeed, Restaurant365, MarketMan, Choco, Tilby*) e Criteri di Accettazione Given/When/Then.
- [`execution_plan.json`](./execution_plan.json) — Piano architetturale e contratti di dominio.
- [`src/AuraEngine/AuraDomain.cs`](./src/AuraEngine/AuraDomain.cs) — Motore di dominio C# (Distinta Base, Auto-86, Procurement AI Broker, UoM Converter, Sentiment Matrix, Live P&L).
- [`tests/AuraEngine.Tests/AuraDomainTests.cs`](./tests/AuraEngine.Tests/AuraDomainTests.cs) — Suite di verifica automatizzata (`11/11` test superati in sandbox).
- [`web/prototype-food-erp.html`](./web/prototype-food-erp.html) — Prototipo interattivo completo (Desktop Web, Tablet iPad, Smartphone iPhone).
- [`Dockerfile`](./Dockerfile) & [`k8s/deployment.yaml`](./k8s/deployment.yaml) — Container multi-stage hardened (`uid: 1000`, `readOnlyRootFilesystem`) e manifest Kubernetes Canary.
