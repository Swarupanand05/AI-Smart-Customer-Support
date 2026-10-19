# AI-CARE: AI-Driven Customer Assistance & Real-Time Escalation Framework

[![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-10.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![React](https://img.shields.io/badge/React-19.0-61DAFB?style=for-the-badge&logo=react&logoColor=black)](https://react.dev/)
[![Python](https://img.shields.io/badge/Python-Flask%20%7C%20Scikit--Learn-3776AB?style=for-the-badge&logo=python&logoColor=white)](https://www.python.org/)
[![MongoDB](https://img.shields.io/badge/MongoDB-Atlas%20%7C%20Local-47A248?style=for-the-badge&logo=mongodb&logoColor=white)](https://www.mongodb.com/)
[![SQL Server](https://img.shields.io/badge/SQL%20Server-LocalDB%20%7C%20EF%20Core-CC292B?style=for-the-badge&logo=microsoftsqlserver&logoColor=white)](https://www.microsoft.com/sql-server)
[![License](https://img.shields.io/badge/License-MIT-blue.svg?style=for-the-badge)](LICENSE)

---

## 📌 Executive Summary

**AI-CARE** (AI-Driven Customer Assistance and Real-Time Escalation) is an enterprise-grade, full-stack customer support platform designed to streamline customer communications, accurately categorize incoming inquiries, gauge sentiment, dynamically prioritize issues, and escalate complex requests into manageable support tickets.

The platform bridges real-time automated conversational intelligence with administrative workflows, combining a **hybrid machine learning classifier** with rule-based domain guardrails, stateful conversational session tracking, and high-performance database indexing.

---

## 🏛 Architecture Overview

AI-CARE employs a distributed, service-oriented architecture ensuring separation of concerns, scalability, and resilience:

```mermaid
graph TD
    User([Customer / Client]) <-->|HTTP / JSON| Frontend[React 19 Frontend SPA]
    Admin([Support Agent / Admin]) <-->|HTTP / JSON| Frontend

    subgraph "Application Layer"
        Frontend <-->|REST API + JWT| Backend[ASP.NET Core 10 Web API]
        Backend <-->|Internal HTTP :5001| ML[Python Flask ML Microservice]
        Backend <-->|External LLM API| Groq[Groq / Gemini AI Fallback]
    end

    subgraph "Data Persistence"
        Backend <-->|EF Core 10| SQL[(SQL Server LocalDB - Users & Auth)]
        Backend <-->|MongoDB Driver 3.x| Mongo[(MongoDB - Tickets, Chats, Sessions)]
    end

    subgraph "ML Microservice (:5001)"
        ML --> Preprocess[NLP Normalizer & Preprocessor]
        Preprocess --> Hybrid[Hybrid Decision Engine]
        Hybrid --> Rules[High-Precision Regex Guardrails]
        Hybrid --> Scikit[TF-IDF + Balanced Logistic Regression]
        Hybrid --> Sentiment[Sentiment Pipeline]
        Hybrid --> MetricsCache[In-Memory Held-out Metrics Cache]
    end
```

### Core Architecture Components
1. **Frontend (`SPT/Frontend/login-app`)**:
   - Built with **React 19** and modern Vanilla CSS design systems.
   - User Chatbot with session-aware chat history, typing indicators, quick replies, and automatic action prompts.
   - Comprehensive **Admin Dashboard** featuring real-time KPI metrics, sentiment breakdowns, priority filtering, ticket inspectors, and live ML performance evaluation modals.
2. **Backend API (`SPT/dotnet-api/SupportAPI`)**:
   - Built on **ASP.NET Core 10**.
   - Handles authentication, JWT issuance, password hashing, and role-based authorization.
   - Implements multi-turn conversational context inheritance (`ChatSessionController`), ticket lifecycle management (`TicketController`), and duplicate prevention.
   - Communicates with the ML microservice via non-blocking HTTP (`MLService`).
3. **Machine Learning Microservice (`SPT/ML_API`)**:
   - Built with **Python Flask** and **Scikit-Learn**.
   - Features a **Hybrid Operational Classifier** combining high-precision regex pattern matching with a balanced Logistic Regression model ($C=5.0$) trained on clean, deduplicated support corpora.
   - Sentiment analysis classifier trained on 24,000+ customer reviews and social support queries.
   - Cached held-out metrics endpoint (`/metrics`) responding in under 5ms.
4. **Data Stores**:
   - **SQL Server LocalDB**: User profiles, credentials, and authentication tables via Entity Framework Core.
   - **MongoDB**: High-throughput storage for tickets, conversation turns, and multi-turn chat sessions with compound indexing on `(userEmail, createdAt)` and `(status, priority)`.

---

## 📁 Repository Directory Structure

```text
AI-Smart-Customer-Support/
├── .github/                       # GitHub workflows and CI configurations
├── .gitignore                     # Universal gitignore (.NET, Node, Python, Datasets)
├── README.md                      # Primary project documentation (this file)
├── SPT/                           # System Project Trunk (Source Code)
│   ├── SPT.sln                    # Visual Studio Solution File
│   │
│   ├── dotnet-api/                # Backend API (.NET 10)
│   │   └── SupportAPI/
│   │       ├── Controllers/       # API Controllers (Chat, Ticket, Session, Auth)
│   │       │   ├── AuthController.cs
│   │       │   ├── ChatController.cs
│   │       │   ├── ChatSessionController.cs
│   │       │   └── TicketController.cs
│   │       ├── Data/              # EF Core DB Context (LocalDB)
│   │       │   └── AppDbContext.cs
│   │       ├── Model/             # Data Entities (DTOs & Database Models)
│   │       │   ├── ChatMessage.cs
│   │       │   ├── ChatSession.cs
│   │       │   ├── Ticket.cs
│   │       │   └── User.cs
│   │       ├── Services/          # Core Business Logic & External Clients
│   │       │   ├── AIService.cs       # LLM fallback (Groq / Gemini)
│   │       │   ├── EmailService.cs    # SMTP notifications
│   │       │   ├── JwtService.cs      # Token issuance and validation
│   │       │   ├── MLService.cs       # ML microservice connector & health
│   │       │   └── MongoService.cs    # MongoDB repository layer
│   │       ├── appsettings.json   # Connection strings and configuration
│   │       ├── Program.cs         # Dependency injection and middleware pipeline
│   │       └── SupportAPI.csproj  # .NET project definitions
│   │
│   ├── Frontend/                  # User Interfaces
│   │   ├── Admin Dashboard/       # Standalone Admin Dashboard components
│   │   ├── Chatbot/               # Standalone Chatbot components
│   │   └── login-app/             # Complete Single Page Application (React 19)
│   │       ├── public/            # Static HTML, manifest, and icons
│   │       ├── src/
│   │       │   ├── components/    # Reusable UI Components
│   │       │   │   ├── AdminDashboard.jsx  # Real-time metrics & management
│   │   │   │   ├── AdminDashboard.css  # Dashboard glassmorphism styles
│   │   │   │   ├── AdminRoute.jsx      # Role-based route guard
│   │   │   │   ├── Chatbot.jsx         # Interactive floating support widget
│   │   │   │   ├── Chatbot.css         # Chatbot theme & animations
│   │   │   │   ├── ChatSidebar.jsx     # Session history sidebar
│   │   │   │   ├── Dashboard.js        # User home dashboard
│   │   │   │   ├── Login.jsx           # Secure authentication form
│   │   │   │   └── Register.jsx        # Account registration
│   │       │   ├── services/      # Frontend API clients (Axios/Fetch)
│   │       │   │   └── api.js
│   │       │   ├── App.js         # Client-side router configuration
│   │       │   └── index.js       # React root mount point
│   │       └── package.json       # Node package dependencies
│   │
│   └── ML_API/                    # Machine Learning Service (Python Flask)
│       ├── app.py                 # Microservice routes, models, and training
│       ├── category_model.pkl     # Trained category classifier (TF-IDF + LogReg)
│       ├── sentiment_model.pkl    # Trained sentiment classifier
│       ├── independent_benchmark.json # Independent evaluation set (N=160)
│       ├── provisional_benchmark.json # Development regression benchmark (N=80)
│       ├── requirements.txt       # Python package requirements
│       ├── all_tickets_processed_improved_v3.csv  # IT tickets corpus
│       ├── customer_support_tickets.csv           # E-commerce tickets corpus
│       ├── twitter_training.csv   # Sentiment training data
│       └── twitter_validation.csv # Sentiment validation data
│
└── SRS/                           # System Requirement Specifications & Academic Docs
    ├── Project Report Template.docx
    └── ...
```

---

## ⚡ Key Features & Innovations

### 1. Hybrid Operational Classifier
Customer inquiries are processed through a two-stage classification engine:
1. **High-Precision Pattern Guardrails**: Regex-based domain detectors catch deterministic actions (e.g., refund demands, tracking numbers, coupon requests, login resets).
2. **Balanced Machine Learning Pipeline**: Unmatched queries pass to a Scikit-Learn pipeline featuring character and word n-grams (1-3) with class-balanced Logistic Regression ($C=5.0$) trained across 8 core categories:
   - `Account` | `Billing` | `General` | `Product` | `Promo` | `Returns` | `Shipping` | `Technical`
3. **Safety Fallback**: Queries with low ML confidence ($< 0.60$) and absent keywords safely default to `General` to avoid erroneous automated actions.

### 2. Contextual Conversation & Category Inheritance
In real-world customer support, follow-up messages rarely restate the problem category. For example:
- *Turn 1*: "I want to return my damaged shoes." $\rightarrow$ **Category: Returns**
- *Turn 2*: "Here is my order number: OD-98234120." $\rightarrow$ **Category: Returns (Inherited)**
- *Turn 3*: "What should I do next?" $\rightarrow$ **Category: Returns (Inherited)**

AI-CARE tracks active sessions via `ChatSessionController` and inherits the previous active category when follow-up messages lack conflicting category intent or contain order identifiers.

### 3. Real-Time Dynamic Priority Matrix
Every inquiry is mapped to an operational priority (`Low`, `Medium`, `High`, `Critical`) determined dynamically by category severity combined with detected sentiment:

| Category | Positive Sentiment | Neutral Sentiment | Negative Sentiment |
| :--- | :---: | :---: | :---: |
| **Billing** | Medium | High | **Critical** |
| **Returns** | Low | Medium | **High** |
| **Account** | Low | Medium | **High** |
| **Technical** | Medium | Medium | **High** |
| **Shipping** | Low | Medium | **High** |
| **Product / Promo** | Low | Low | **Medium** |
| **General** | Low | Low | **Low** |

### 4. Duplicate Ticket Prevention
To avoid ticket spamming, the `TicketController` analyzes new ticket submissions against open issues from the same user within a sliding time window (30 minutes). If an open ticket with identical content or category already exists, the API links the conversation rather than generating duplicate work items.

---

## 📊 Model Evaluation & Reproducibility Audit (Phase 2.3.3 Canonical)

AI-CARE has undergone a complete, auditable evaluation across multiple benchmarks to ensure mathematical integrity, zero data leakage, and reproducible classification metrics. All reported numbers are computed by a single canonical evaluation engine (`SPT/ML_API/canonical_evaluator.py`) with automated mathematical assertions.

### Benchmark Performance Summary

| Evaluation Set ($N$) | Evaluation Scope & Notes | Cross-Split Leakage | Raw ML Accuracy | Raw ML Macro-F1 | Operational Accuracy | Operational Macro-F1 |
| :--- | :--- | :---: | :---: | :---: | :---: | :---: |
| **Historical Split ($N = 2,098$)** | *Contaminated reference* (4x synthetic replication) | 6.67% *(140 duplicate rows)* | 57.01% | 0.7067 | N/A | N/A |
| **Clean Held-Out Split ($N = 1,986$)** | 15% stratified test split on 13,239 unique rows | **0.00%** *(Strictly isolated)* | 53.47% | 0.4469 | 43.00% *(safe sink)* | 0.4327 |
| **Development Benchmark ($N = 80$)** | Curated 10/class set (used during rule iteration) | N/A *(Internal dev set)* | 65.00% | 0.6595 | 81.25% | 0.8182 |
| **Independent Benchmark ($N = 160$)** | Balanced 20/class untouched validation set | **0.00%** *(Strictly isolated)* | 43.75% | 0.4414 | **61.25%** | **0.6322** |
| **Sentiment Held-Out Split ($N = 3,650$)** | 15% stratified test split on 24,328 reviews & tweets | **0.00%** *(Strictly isolated)* | 75.10% | 0.7414 | 75.10% | 0.7414 |

> [!NOTE]
> **Why Operational Accuracy is 43.00% on the Clean Kaggle Held-Out Split:**
> The Kaggle/IT Helpdesk dataset contains raw stack traces and internal kernel crash logs lacking conversational e-commerce keywords. When machine learning confidence falls below $0.60$ and no domain keyword matches, the operational classifier safely routes inquiries to `General` (which received 598 predictions). This safety fallback prevents erroneous automated refunds or account modifications on ambiguous messages.

---

### Independent Operational Benchmark Breakdown ($N = 160$, Balanced 20/Class)

The independent operational benchmark consists of 160 realistic customer inquiries (20 per category), held strictly separate from rule engineering. Evaluated with `canonical_evaluator.py`:

| Category | True Support | Pred Count | TP | FP | FN | Precision | Recall | F1-Score | Operational Characteristics |
| :--- | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :--- |
| **Account** | 20 | 12 | 11 | 1 | 9 | 0.9167 | 0.5500 | **0.6875** | High Precision, credential reset detection |
| **Billing** | 20 | 10 | 9 | 1 | 11 | 0.9000 | 0.4500 | **0.6000** | Catches unauthorized charges & invoices |
| **General** | 20 | 42 | 12 | 30 | 8 | 0.2857 | 0.6000 | **0.3871** | Conservative safe fallback sink |
| **Product** | 20 | 8 | 7 | 1 | 13 | 0.8750 | 0.3500 | **0.5000** | High Precision on product questions |
| **Promo** | 20 | 18 | 18 | 0 | 2 | 1.0000 | 0.9000 | **0.9474** | Robust coupon & discount extraction |
| **Returns** | 20 | 14 | 11 | 3 | 9 | 0.7857 | 0.5500 | **0.6471** | Reliable refund & exchange tracking |
| **Shipping** | 20 | 18 | 14 | 4 | 6 | 0.7778 | 0.7000 | **0.7368** | Fast courier & tracking matching |
| **Technical** | 20 | 38 | 16 | 22 | 4 | 0.4211 | 0.8000 | **0.5517** | Broad technical bug detection |
| **Overall (Unweighted)** | **160** | **160** | **98** | **62** | **62** | **0.7452** | **0.6125** | **0.6322** | **+17.5% Accuracy over raw ML** |

$$\text{Accuracy} = \frac{\text{Trace}(C)}{N} = \frac{11 + 9 + 12 + 7 + 18 + 11 + 14 + 16}{160} = \frac{98}{160} = \mathbf{0.6125}\ (61.25\%)$$

$$\text{Macro-F1} = \frac{0.6875 + 0.6000 + 0.3871 + 0.5000 + 0.9474 + 0.6471 + 0.7368 + 0.5517}{8} = \mathbf{0.632200}$$

#### Operational Pipeline Confusion Matrix ($N = 160$)
*Rows represent True Ground-Truth Categories; Columns represent Predicted Categories:*

| True Category \ Pred | Account | Billing | General | Product | Promo | Returns | Shipping | Technical | **Row Sum (True Support)** |
| :--- | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: |
| **Account** | **11** | 0 | 5 | 0 | 0 | 1 | 1 | 2 | **20** |
| **Billing** | 0 | **9** | 10 | 0 | 0 | 0 | 0 | 1 | **20** |
| **General** | 0 | 0 | **12** | 0 | 0 | 1 | 0 | 7 | **20** |
| **Product** | 0 | 0 | 4 | **7** | 0 | 1 | 0 | 8 | **20** |
| **Promo** | 1 | 0 | 0 | 0 | **18** | 0 | 0 | 1 | **20** |
| **Returns** | 0 | 0 | 4 | 0 | 0 | **11** | 3 | 2 | **20** |
| **Shipping** | 0 | 0 | 5 | 0 | 0 | 0 | **14** | 1 | **20** |
| **Technical** | 0 | 1 | 2 | 1 | 0 | 0 | 0 | **16** | **20** |
| **Col Sum (Predicted)**| **12** | **10** | **42** | **8** | **18** | **14** | **18** | **38** | **160** |

*Matrix Trace (Sum of Diagonal):* $11 + 9 + 12 + 7 + 18 + 11 + 14 + 16 = 98$.

---

### Audit Assertions & Reconciliation Details

Every evaluation calculation in AI-CARE satisfies automated mathematical checks:
1. $\sum \text{True Support} = \sum \text{Prediction Counts} = \sum \text{Row Sums} = \sum \text{Col Sums} = N$.
2. $\text{Accuracy} = \frac{\text{Trace}(C)}{N} = \frac{\sum_{i=1}^8 C_{i,i}}{N}$.
3. $\text{Precision}_i = \frac{C_{i,i}}{\text{ColSum}_i}$, $\text{Recall}_i = \frac{C_{i,i}}{\text{RowSum}_i}$, $\text{F1}_i = \frac{2 \cdot P_i \cdot R_i}{P_i + R_i}$.
4. $\text{Macro-F1}$ strictly equals the unweighted arithmetic mean of all 8 per-class F1-scores.
5. $\text{Weighted-F1}$ strictly equals the support-weighted average $\frac{\sum S_i \cdot \text{F1}_i}{N}$.

#### Explanation of Historical Inconsistencies:
- **Historical Mean of 0.7067 vs 0.7693 Prototype**: In Phase 2.1 exploratory testing, an early prototype table with 8 F1 values averaging 0.7693 was documented in text. The underlying model array evaluated on the historical contaminated test set ($N=2,098$) had support $[251, 257, 9, 250, 16, 504, 27, 784]$ and F1s $[0.9139, 0.2651, 1.0000, 0.2486, 1.0000, 0.4409, 1.0000, 0.7851]$ whose exact unweighted mean is $\frac{5.6536}{8} = \mathbf{0.706700}$. The 0.7693 figure was an obsolete text draft artifact.
- **Historical 1.0000 F1 on Minority Classes**: The old dataset replicated 246 synthetic sentences $4\times$ prior to random splitting, leaking 140 identical sentences into the test set. This created artificial $1.0000$ scores for `General`, `Promo`, and `Shipping`. In the clean, deduplicated dataset ($0.00\%$ cross-split leakage), genuine held-out F1 scores are reported without artificial inflation.
- **Evaluation Independence Disclaimer**: The 160-sample operational benchmark is an internally curated evaluation set created without keyword tuning overlap. It has not been annotated or certified by a third-party audit firm. No unverified claims of "91.99% category accuracy" or "88.76% sentiment accuracy" are asserted.


## 🚀 Getting Started

### Prerequisites
- **.NET 10 SDK** installed (`dotnet --version`)
- **Node.js (v18+)** and **npm** (`node -v`, `npm -v`)
- **Python 3.10+** (`python --version`)
- **MongoDB** running locally on port `27017` (or MongoDB Atlas connection URI)
- **SQL Server LocalDB** (included with Visual Studio / .NET tools)

---

### Step 1: Clone the Repository
```bash
git clone https://github.com/Swarupanand05/AI-Smart-Customer-Support.git
cd AI-Smart-Customer-Support
```

---

### Step 2: Start the Python ML Microservice
The ML service provides prediction and metrics endpoints on `http://127.0.0.1:5001`.

```bash
cd SPT/ML_API
python -m venv venv
# Windows:
.\venv\Scripts\activate
# Linux/macOS:
source venv/bin/activate

pip install -r requirements.txt
python app.py
```
*The service will load the models and output:*
```text
============================================================
  AI Smart Customer Support — ML API Starting
============================================================
[Category] Clean unique training samples: 13239
[Category] Loading cached model from category_model.pkl
============================================================
  Models ready. Serving on port 5001
============================================================
```

---

### Step 3: Start the ASP.NET Core Backend API
The backend Web API runs on `http://localhost:5273`.

```bash
cd SPT/dotnet-api/SupportAPI
dotnet restore
dotnet build
dotnet run
```
*Health check endpoint:* `http://localhost:5273/api/chat/health`

---

### Step 4: Start the React Frontend Application
The frontend runs on `http://localhost:3000`.

```bash
cd SPT/Frontend/login-app
npm install
npm start
```

---

## 🔑 Default Credentials

For demonstration and administrative inspection:

| Role | Email | Password | Access Level |
| :--- | :--- | :--- | :--- |
| **Admin** | `admin@support.com` | `Admin@123` | Full Admin Dashboard, Ticket Management, Metrics Inspector |
| **Customer** | `customer@example.com` | `Customer@123` | Self-Service Chatbot, Ticket History |

---

## 📡 API Reference

### 1. Chat & Classification Endpoint
- **URL**: `POST /api/chat`
- **Headers**: `Authorization: Bearer <JWT>`, `Content-Type: application/json`
- **Request Body**:
  ```json
  {
    "message": "My card was charged twice for order OD-88231",
    "email": "customer@example.com",
    "sessionId": "6ad0d0b03b31534757324e65"
  }
  ```
- **Response**:
  ```json
  {
    "reply": "I understand you were charged multiple times. I have routed this to our Billing team.",
    "category": "Billing",
    "categoryConf": 94,
    "sentiment": "Negative",
    "priority": "Critical",
    "ticketCreated": false,
    "isGreeting": false
  }
  ```

### 2. Multi-Turn Chat Session
- `POST /api/chatsession` — Create a new customer support session.
- `GET /api/chatsession/user/{email}` — Retrieve session history for a user.
- `POST /api/chatsession/{id}/message` — Append turns to preserve context.

### 3. Ticket Management
- `POST /api/ticket` — Create an escalated ticket (with duplicate checks).
- `GET /api/ticket` — Retrieve all open tickets (Admin only).
- `PUT /api/ticket/{id}/status` — Update ticket status (`Open`, `In Progress`, `Resolved`, `Closed`).

### 4. ML Metrics & Audit Endpoint
- **URL**: `GET http://127.0.0.1:5001/metrics`
- Returns verified accuracy, Macro-F1, per-class confusion matrices, and benchmark reports.

---

## 🧪 Comprehensive Verification & Testing

AI-CARE includes an automated verification suite covering 14 integration scenarios:

```bash
# In SPT directory with backend & ML service active:
python -c "
import urllib.request, json
res = urllib.request.urlopen('http://localhost:5273/api/chat/health')
print('Backend Health:', json.loads(res.read()))
"
```

### Verified Integration Scenarios
- [x] **Scenario 1**: Greeting & FAQ detection (`isGreeting=True`, `isFaq=True`)
- [x] **Scenario 2**: Billing complaint triage (`Category=Billing`, `Priority=Critical`)
- [x] **Scenario 3**: Account login & credential reset handling (`Category=Account`)
- [x] **Scenario 4**: Shipping delay tracking (`Category=Shipping`, `Priority=High`)
- [x] **Scenario 5**: Product & promotional discount inquiry (`Category=Promo`)
- [x] **Scenario 6**: Negative sentiment escalation (`Sentiment=Negative`)
- [x] **Scenario 7**: Contextual category inheritance without keyword restatement
- [x] **Scenario 8**: Order ID follow-up context preservation (`OD-XXXXX`)
- [x] **Scenario 9**: Unresolved request automated ticket creation
- [x] **Scenario 10**: Ambiguous message graceful handling (`General` fallback)
- [x] **Scenario 11**: Python ML Service availability & `/metrics` integrity
- [x] **Scenario 12**: MongoDB query performance & index resilience ($< 30\text{ ms}$)
- [x] **Scenario 13**: Payload input validation & HTTP 400 Bad Request guardrails
- [x] **Scenario 14**: Duplicate ticket submission prevention

---

## 📄 License

This project is licensed under the **MIT License** — see the [LICENSE](LICENSE) file for details.

---

## 👥 Contributors & Contact

- **Lead Developer**: Swarupanand
- **Repository**: [https://github.com/Swarupanand05/AI-Smart-Customer-Support](https://github.com/Swarupanand05/AI-Smart-Customer-Support)
- **Institution**: Final Year Capstone Project — Computer Science & Engineering
