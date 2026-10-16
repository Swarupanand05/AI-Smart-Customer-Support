"""
Generate the corrected SRS Word document for AI Smart Customer Support System.
Run: python generate_srs.py
Requires: pip install python-docx Pillow
"""

import shutil, os
from docx import Document
from docx.shared import Inches, Pt, RGBColor, Cm
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.style import WD_STYLE_TYPE
from docx.oxml.ns import qn
from docx.oxml import OxmlElement

# ── Paths ─────────────────────────────────────────────────────────────────────
SCRIPT_DIR  = os.path.dirname(os.path.abspath(__file__))
ARTIFACTS   = r"C:\Users\bswar\.gemini\antigravity-ide\brain\53730ebd-7616-4734-9acb-2d5126c1edc5"
OUTPUT_DOCX = os.path.join(SCRIPT_DIR, "SRS_AI_Smart_Customer_Support_CORRECTED.docx")

# Image file names (from artifacts directory)
IMAGES = {
    "architecture": None,
    "er":           None,
    "class":        None,
    "sequence":     None,
    "usecase":      None,
    "activity":     None,
}

# Find the generated images
for fname in os.listdir(ARTIFACTS):
    if "architecture_diagram_corrected" in fname and fname.endswith(".png"):
        IMAGES["architecture"] = os.path.join(ARTIFACTS, fname)
    elif "er_diagram_corrected" in fname and fname.endswith(".png"):
        IMAGES["er"] = os.path.join(ARTIFACTS, fname)
    elif "class_diagram_corrected" in fname and fname.endswith(".png"):
        IMAGES["class"] = os.path.join(ARTIFACTS, fname)
    elif "sequence_diagram_corrected" in fname and fname.endswith(".png"):
        IMAGES["sequence"] = os.path.join(ARTIFACTS, fname)
    elif "usecase_diagram_corrected" in fname and fname.endswith(".png"):
        IMAGES["usecase"] = os.path.join(ARTIFACTS, fname)
    elif "activity_diagram_corrected" in fname and fname.endswith(".png"):
        IMAGES["activity"] = os.path.join(ARTIFACTS, fname)

print("Found images:")
for k, v in IMAGES.items():
    print(f"  {k}: {v}")

# ── Document helpers ──────────────────────────────────────────────────────────
def set_cell_bg(cell, hex_color):
    tc   = cell._tc
    tcPr = tc.get_or_add_tcPr()
    shd  = OxmlElement("w:shd")
    shd.set(qn("w:val"), "clear")
    shd.set(qn("w:color"), "auto")
    shd.set(qn("w:fill"), hex_color)
    tcPr.append(shd)

def add_heading(doc, text, level=1, color="1a237e"):
    para = doc.add_heading(text, level=level)
    para.alignment = WD_ALIGN_PARAGRAPH.LEFT
    for run in para.runs:
        run.font.color.rgb = RGBColor.from_string(color)
    return para

def add_body(doc, text, bold=False, size=11):
    para = doc.add_paragraph()
    run  = para.add_run(text)
    run.font.size = Pt(size)
    run.bold = bold
    return para

def add_bullet(doc, text, level=0):
    para = doc.add_paragraph(style="List Bullet")
    para.paragraph_format.left_indent = Inches(0.3 * (level + 1))
    run = para.add_run(text)
    run.font.size = Pt(10.5)
    return para

def add_image(doc, path, caption, width=Inches(6.2)):
    if path and os.path.exists(path):
        doc.add_picture(path, width=width)
        last = doc.paragraphs[-1]
        last.alignment = WD_ALIGN_PARAGRAPH.CENTER
        cap = doc.add_paragraph(caption)
        cap.alignment = WD_ALIGN_PARAGRAPH.CENTER
        for run in cap.runs:
            run.font.size   = Pt(9)
            run.font.italic = True
            run.font.color.rgb = RGBColor(100, 100, 100)
    else:
        p = doc.add_paragraph(f"[IMAGE MISSING: {caption}]")
        p.alignment = WD_ALIGN_PARAGRAPH.CENTER

def add_table_row(table, cells, header=False, bg=None):
    row = table.add_row()
    for i, text in enumerate(cells):
        cell = row.cells[i]
        cell.text = text
        for run in cell.paragraphs[0].runs:
            run.font.size = Pt(10)
            run.bold      = header
        if bg:
            set_cell_bg(cell, bg)
    return row

# ── Build document ────────────────────────────────────────────────────────────
doc = Document()

# Page margins
for section in doc.sections:
    section.top_margin    = Cm(2.0)
    section.bottom_margin = Cm(2.0)
    section.left_margin   = Cm(2.5)
    section.right_margin  = Cm(2.5)

# ─── COVER PAGE ───────────────────────────────────────────────────────────────
doc.add_paragraph()
title = doc.add_paragraph()
title.alignment = WD_ALIGN_PARAGRAPH.CENTER
run = title.add_run("SOFTWARE REQUIREMENTS SPECIFICATION (SRS)")
run.font.size  = Pt(20)
run.font.bold  = True
run.font.color.rgb = RGBColor(26, 35, 126)

doc.add_paragraph()
sub = doc.add_paragraph()
sub.alignment = WD_ALIGN_PARAGRAPH.CENTER
run2 = sub.add_run("AI Smart Customer Support System")
run2.font.size  = Pt(16)
run2.font.bold  = True
run2.font.color.rgb = RGBColor(21, 101, 192)

doc.add_paragraph()
for line in [
    "Version: 2.0 (Corrected)",
    "Date: July 2026",
    "Prepared by: Development Team",
    "Technology Stack: React 18 | .NET 10 | Python Flask | MongoDB | SQL Server | Google Gemini AI",
]:
    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    r = p.add_run(line)
    r.font.size = Pt(11)

doc.add_page_break()

# ─── TABLE OF CONTENTS ────────────────────────────────────────────────────────
add_heading(doc, "Table of Contents", 1)
toc_items = [
    "1. Introduction",
    "   1.1 Purpose",
    "   1.2 Scope",
    "   1.3 Definitions, Acronyms & Abbreviations",
    "   1.4 Overview",
    "2. Overall Description",
    "   2.1 Product Perspective",
    "   2.2 Product Functions",
    "   2.3 User Classes & Characteristics",
    "   2.4 Operating Environment",
    "   2.5 Design & Implementation Constraints",
    "3. System Architecture",
    "   3.1 Architecture Block Diagram",
    "   3.2 Component Descriptions",
    "4. Specific Requirements",
    "   4.1 Functional Requirements",
    "   4.2 Non-Functional Requirements",
    "5. UML Diagrams",
    "   5.1 Use Case Diagram",
    "   5.2 Class Diagram",
    "   5.3 Sequence Diagram",
    "   5.4 Activity Diagram",
    "   5.5 ER Diagram",
    "6. API Reference",
    "7. Database Design",
    "8. ML Models",
    "9. Security",
    "10. Assumptions & Dependencies",
]
for item in toc_items:
    p = doc.add_paragraph(item)
    p.paragraph_format.left_indent = Inches(0.3 if item.startswith(" ") else 0)
    for run in p.runs:
        run.font.size = Pt(10.5)

doc.add_page_break()

# ─── SECTION 1: INTRODUCTION ──────────────────────────────────────────────────
add_heading(doc, "1. Introduction", 1)

add_heading(doc, "1.1 Purpose", 2)
add_body(doc, (
    "This Software Requirements Specification (SRS) describes the requirements for the "
    "AI Smart Customer Support System — a full-stack web application that automates customer "
    "support using Artificial Intelligence, Machine Learning, and a multi-tier ticketing system. "
    "This document serves as a reference for developers, testers, and stakeholders."
))

add_heading(doc, "1.2 Scope", 2)
add_body(doc, (
    "The AI Smart Customer Support System provides the following capabilities:"
))
for item in [
    "AI-powered chatbot with instant FAQ answers (30+ keyword patterns) and Google Gemini API fallback",
    "Machine Learning text classification: auto-detect category (Billing, Technical, Account, Shipping, General) and sentiment (Positive, Negative, Neutral)",
    "Automatic support ticket creation for non-FAQ queries with ML-predicted priority",
    "Full conversation thread system: admin replies and user follow-up replies on tickets",
    "Role-based access control: regular users and admin portal",
    "Analytics dashboard for admins: ticket stats, trends, resolution time",
]:
    add_bullet(doc, item)

add_heading(doc, "1.3 Definitions, Acronyms & Abbreviations", 2)
table = doc.add_table(rows=1, cols=2)
table.style = "Table Grid"
hdr = table.rows[0].cells
hdr[0].text = "Term"
hdr[1].text = "Definition"
for cell in hdr:
    for run in cell.paragraphs[0].runs:
        run.bold = True
    set_cell_bg(cell, "1a237e")
    for run in cell.paragraphs[0].runs:
        run.font.color.rgb = RGBColor(255,255,255)

definitions = [
    ("SRS",       "Software Requirements Specification"),
    ("JWT",       "JSON Web Token — used for stateless authentication"),
    ("API",       "Application Programming Interface"),
    ("REST",      "Representational State Transfer"),
    ("ML",        "Machine Learning"),
    ("NLP",       "Natural Language Processing"),
    ("TF-IDF",    "Term Frequency–Inverse Document Frequency (text vectorization)"),
    ("LLM",       "Large Language Model (Google Gemini)"),
    ("FAQ",       "Frequently Asked Questions"),
    ("MongoDB",   "NoSQL document database for tickets and chats"),
    ("EF Core",   "Entity Framework Core — ORM for SQL Server"),
    ("SHA-256",   "Secure Hash Algorithm 256-bit (password hashing)"),
    ("CORS",      "Cross-Origin Resource Sharing"),
    ("DTO",       "Data Transfer Object"),
]
for term, defn in definitions:
    row = table.add_row()
    row.cells[0].text = term
    row.cells[1].text = defn
    for cell in row.cells:
        for run in cell.paragraphs[0].runs:
            run.font.size = Pt(10)

doc.add_paragraph()

add_heading(doc, "1.4 Overview", 2)
add_body(doc, (
    "This SRS is organized into 10 sections covering introduction, system description, architecture, "
    "functional and non-functional requirements, UML diagrams, API reference, database design, "
    "ML model details, and security specifications."
))

doc.add_page_break()

# ─── SECTION 2: OVERALL DESCRIPTION ──────────────────────────────────────────
add_heading(doc, "2. Overall Description", 1)

add_heading(doc, "2.1 Product Perspective", 2)
add_body(doc, (
    "The system is a three-tier web application:"
))
for item in [
    "Tier 1 — Presentation Layer: React 18 SPA served on port 3000",
    "Tier 2 — Business Logic Layer: .NET 10 ASP.NET Core REST API on port 5273",
    "Tier 3 — Data Layer: SQL Server (users) + MongoDB (tickets, chats)",
    "Supporting Service — Python Flask ML API on port 5001",
    "External Service — Google Gemini API (cloud LLM)",
]:
    add_bullet(doc, item)

add_heading(doc, "2.2 Product Functions", 2)
functions = [
    ("User Registration & Login",        "SHA-256 password hashing, JWT token generation, role-based redirect"),
    ("AI Chatbot",                        "30+ FAQ patterns for instant answers; Google Gemini for complex queries"),
    ("ML Classification",                 "Python Flask API classifies every message: category + sentiment + confidence%"),
    ("Auto Ticket Creation",             "Non-FAQ queries automatically create a ticket in MongoDB with ML-assigned category and priority"),
    ("Ticket Conversation Thread",       "Full back-and-forth chat: admin sends reply, user can reply back; Messages[] array in MongoDB"),
    ("Admin Dashboard",                  "View/filter all tickets, reply with status change, analytics overview"),
    ("Analytics",                         "Total/open/resolved counts, 7-day trend, per-category breakdown, avg resolution time"),
]
table = doc.add_table(rows=1, cols=2)
table.style = "Table Grid"
for i, text in enumerate(["Feature", "Description"]):
    hdr = table.rows[0].cells[i]
    hdr.text = text
    for run in hdr.paragraphs[0].runs:
        run.bold = True
    set_cell_bg(hdr, "283593")
    for run in hdr.paragraphs[0].runs:
        run.font.color.rgb = RGBColor(255,255,255)
for feat, desc in functions:
    row = table.add_row()
    row.cells[0].text = feat
    row.cells[1].text = desc
    for cell in row.cells:
        for run in cell.paragraphs[0].runs:
            run.font.size = Pt(10)

doc.add_paragraph()

add_heading(doc, "2.3 User Classes & Characteristics", 2)
add_body(doc, "Two user classes exist in the system:", bold=True)
add_body(doc, "Regular User:", bold=True)
for item in [
    "Registers with email/password",
    "Can chat with the AI chatbot",
    "Views own ticket history and conversation threads",
    "Can reply to admin messages on their tickets",
]:
    add_bullet(doc, item, level=1)

add_body(doc, "Administrator:", bold=True)
for item in [
    "Hardcoded credentials: admin@support.com / Admin@123",
    "Views all tickets across all users",
    "Filters tickets by status (Open/Resolved/Closed)",
    "Sends replies and changes ticket status",
    "Views analytics dashboard",
]:
    add_bullet(doc, item, level=1)

add_heading(doc, "2.4 Operating Environment", 2)
for item in [
    "OS: Windows 10/11 (development), cross-platform (production)",
    "Browser: Any modern browser (Chrome, Edge, Firefox)",
    "Frontend: Node.js 18+, npm",
    "Backend: .NET 10 SDK",
    "ML API: Python 3.10+, Flask, scikit-learn, pandas",
    "Database: SQL Server LocalDB (dev), MongoDB 6+ local instance",
    "External: Google Gemini API key required",
]:
    add_bullet(doc, item)

add_heading(doc, "2.5 Design & Implementation Constraints", 2)
for item in [
    "Frontend is a React SPA (no SSR); all routing is client-side",
    "Admin account is hardcoded (not in database) — no admin registration UI",
    "CORS is restricted to localhost:3000 (development configuration)",
    "Gemini API calls have a 15-second timeout; retries on rate-limit (429) errors",
    "ML API must be running on port 5001 before the .NET API receives chat messages",
    "JWT secret key is stored in appsettings.json (move to environment variable for production)",
]:
    add_bullet(doc, item)

doc.add_page_break()

# ─── SECTION 3: ARCHITECTURE ──────────────────────────────────────────────────
add_heading(doc, "3. System Architecture", 1)

add_heading(doc, "3.1 Architecture Block Diagram", 2)
add_body(doc, (
    "The following diagram shows the complete system architecture with all layers, services, "
    "data stores, and external integrations:"
))
doc.add_paragraph()
add_image(doc, IMAGES["architecture"],
          "Figure 1: AI Smart Customer Support System — Architecture Block Diagram",
          width=Inches(6.5))
doc.add_paragraph()

add_heading(doc, "3.2 Component Descriptions", 2)

components = [
    ("React Frontend (:3000)",
     "Single-Page Application built with React 18 and React Router v6. "
     "Handles Login, Register, User Dashboard (Chatbot + Ticket History), and Admin Dashboard. "
     "All API calls include a JWT Bearer token from localStorage. "
     "Role-based routing: ProtectedRoute for users, AdminRoute for admin."),

    (".NET 10 Core REST API (:5273)",
     "The central backend built with ASP.NET Core. "
     "Exposes REST endpoints under /api/auth, /api/chat, /api/ticket. "
     "Services: JwtService (token generation), AIService (FAQ + Gemini), "
     "MLService (HTTP client to Python :5001), MongoService (MongoDB CRUD)."),

    ("Python Flask ML API (:5001)",
     "A Flask microservice that loads pre-trained scikit-learn pipelines. "
     "POST /predict: returns category + sentiment + confidence scores. "
     "POST /retrain: forces model retraining from CSV datasets. "
     "GET /health: status check. "
     "Models: TF-IDF (30,000 features, bigrams) + Logistic Regression."),

    ("Google Gemini API (Cloud)",
     "External LLM API used when no FAQ pattern matches the user's message. "
     "Tries three models in order: gemini-1.5-flash-latest → gemini-2.0-flash-lite → gemini-2.0-flash. "
     "Retries on HTTP 429 with exponential backoff (5s, 15s, 30s). "
     "System prompt is injected with the ML-predicted category and sentiment for context."),

    ("SQL Server (LocalDB)",
     "Relational database managed via Entity Framework Core. "
     "Stores only the Users table: UserId, FullName, Email, PasswordHash, Role, CreatedAt. "
     "Admin credentials are hardcoded in AuthController and are NOT stored in this database."),

    ("MongoDB (:27017, SupportDB)",
     "NoSQL document database with two collections: "
     "Tickets (user messages, AI classification, status, conversation thread Messages[]) "
     "and Chats (full chat history per user)."),
]

for comp_name, comp_desc in components:
    add_body(doc, f"{comp_name}", bold=True)
    add_body(doc, comp_desc)
    doc.add_paragraph()

doc.add_page_break()

# ─── SECTION 4: SPECIFIC REQUIREMENTS ────────────────────────────────────────
add_heading(doc, "4. Specific Requirements", 1)

add_heading(doc, "4.1 Functional Requirements", 2)

fr_groups = [
    ("FR-AUTH: Authentication & Authorization", [
        "FR-1.1: Users shall register with full name, email, and password. Passwords are hashed with SHA-256 before storage.",
        "FR-1.2: Users shall login with email and password. System returns a JWT token on success.",
        "FR-1.3: Admin shall login with hardcoded credentials (admin@support.com / Admin@123). System returns an Admin-role JWT.",
        "FR-1.4: All API endpoints except /auth/login and /auth/register shall require a valid JWT Bearer token.",
        "FR-1.5: Users with role 'Admin' shall be redirected to /admin. Other users to /dashboard.",
        "FR-1.6: Unauthorized access to /admin shall redirect to login page.",
    ]),
    ("FR-CHAT: AI Chatbot", [
        "FR-2.1: System shall send every user message to the Python ML API (/predict) to obtain category, sentiment, and confidence scores.",
        "FR-2.2: System shall first attempt to match the message against 30+ keyword-based FAQ patterns.",
        "FR-2.3: If a FAQ match is found, system returns the pre-written answer instantly. No ticket is created.",
        "FR-2.4: If no FAQ match, system calls Google Gemini API with a system prompt containing the ML-predicted category and sentiment.",
        "FR-2.5: If Gemini is called, system automatically creates a support ticket in MongoDB with status 'Open'.",
        "FR-2.6: All chat interactions (message + reply + sentiment) are saved to the Chats collection.",
        "FR-2.7: Each chatbot response includes: reply text, category, sentiment, confidence scores, priority, and ticketCreated flag.",
    ]),
    ("FR-TICKET: Ticket Management", [
        "FR-3.1: Users shall view only their own tickets filtered by email in Ticket History.",
        "FR-3.2: Each ticket card shows: priority, status, original message, full conversation thread, creation date, category.",
        "FR-3.3: Admin shall view all tickets across all users with filtering by status.",
        "FR-3.4: Admin shall send a reply to any ticket. The reply is appended to the Messages[] conversation thread.",
        "FR-3.5: Admin shall change ticket status (Open / Resolved / Closed) with each reply.",
        "FR-3.6: When status is set to Resolved or Closed, system sets the resolvedAt timestamp.",
        "FR-3.7: Users shall post follow-up replies to tickets where admin has replied. Reply is appended to Messages[] thread.",
        "FR-3.8: User follow-up replies do NOT change the ticket status.",
        "FR-3.9: Ticket conversation thread shows messages in chronological order with sender label and timestamp.",
        "FR-3.10: 'Reply to Support' button is hidden when ticket status is 'Closed'.",
    ]),
    ("FR-ANALYTICS: Admin Analytics", [
        "FR-4.1: Analytics dashboard shows total, open, resolved, and closed ticket counts.",
        "FR-4.2: Analytics shows high, medium, and low priority breakdown.",
        "FR-4.3: Analytics shows ticket count per category.",
        "FR-4.4: Analytics shows a 7-day trend of tickets created per day.",
        "FR-4.5: Analytics shows average resolution time in hours.",
    ]),
    ("FR-ML: ML Classification", [
        "FR-5.1: ML API classifies messages into: Billing, Technical, Account, Shipping, General.",
        "FR-5.2: ML API classifies sentiment as: Positive, Negative, or Neutral.",
        "FR-5.3: ML API returns confidence scores (0.0–1.0) for both predictions.",
        "FR-5.4: ML models are trained on: Twitter sentiment dataset + Customer Support Tickets CSV + IT Helpdesk CSV.",
        "FR-5.5: Trained models are cached as .pkl files; reloaded on startup without retraining.",
        "FR-5.6: If ML API is unavailable, system falls back to: category='General', sentiment='Neutral', confidence=0.5.",
    ]),
]

for group_title, reqs in fr_groups:
    add_body(doc, group_title, bold=True)
    for req in reqs:
        add_bullet(doc, req)
    doc.add_paragraph()

add_heading(doc, "4.2 Non-Functional Requirements", 2)

nfr = [
    ("Performance",    "Chatbot response time shall be under 3 seconds for FAQ replies. Gemini replies within 15 seconds."),
    ("Availability",   "System shall be available 24/7 (local development configuration)."),
    ("Security",       "Passwords never stored in plain text. JWT tokens validated on every protected request."),
    ("Scalability",    "MongoDB supports horizontal scaling. .NET API is stateless and can be load-balanced."),
    ("Usability",      "UI shall be responsive and accessible on desktop browsers with dark-mode design."),
    ("Reliability",    "Gemini API failures are handled gracefully with fallback messages. ML API timeout is 10 seconds."),
    ("Maintainability","ML models can be retrained without restarting via POST /retrain endpoint."),
    ("Compatibility",  "Frontend compatible with Chrome 90+, Edge 90+, Firefox 88+."),
]
table = doc.add_table(rows=1, cols=2)
table.style = "Table Grid"
for i, txt in enumerate(["Category", "Requirement"]):
    c = table.rows[0].cells[i]
    c.text = txt
    for run in c.paragraphs[0].runs:
        run.bold = True
    set_cell_bg(c, "283593")
    for run in c.paragraphs[0].runs:
        run.font.color.rgb = RGBColor(255,255,255)
for cat, req in nfr:
    row = table.add_row()
    row.cells[0].text = cat
    row.cells[1].text = req
    for cell in row.cells:
        for run in cell.paragraphs[0].runs:
            run.font.size = Pt(10)

doc.add_page_break()

# ─── SECTION 5: UML DIAGRAMS ──────────────────────────────────────────────────
add_heading(doc, "5. UML Diagrams", 1)

add_heading(doc, "5.1 Use Case Diagram", 2)
add_body(doc, (
    "The Use Case Diagram shows interactions between actors (User, Admin) and the system. "
    "The diagram includes all implemented use cases including the new 'Reply to Admin Message' feature."
))
doc.add_paragraph()
add_image(doc, IMAGES["usecase"], "Figure 2: Use Case Diagram")
doc.add_paragraph()

add_heading(doc, "5.2 Class Diagram", 2)
add_body(doc, (
    "The Class Diagram shows all model classes and service classes with their "
    "exact attributes and methods as implemented in the system. "
    "Note the new TicketMessage embedded class and Messages[] list in the Ticket class."
))
doc.add_paragraph()
add_image(doc, IMAGES["class"], "Figure 3: Class Diagram")
doc.add_paragraph()

doc.add_page_break()
add_heading(doc, "5.3 Sequence Diagram", 2)
add_body(doc, (
    "The Sequence Diagram illustrates the complete AI chatbot request flow: "
    "from user input through ML classification, FAQ matching, Gemini API call, "
    "ticket creation, and response delivery. "
    "The Python Flask ML API (:5001) and Google Gemini API are shown as separate participants."
))
doc.add_paragraph()
add_image(doc, IMAGES["sequence"], "Figure 4: Sequence Diagram — AI Chat Flow", width=Inches(6.8))
doc.add_paragraph()

doc.add_page_break()
add_heading(doc, "5.4 Activity Diagram", 2)
add_body(doc, (
    "The Activity Diagram shows the full system workflow with three swimlanes: "
    "User, System (.NET API + ML), and Admin. "
    "It covers authentication, chatbot interaction, ticket creation, admin reply, and user follow-up reply."
))
doc.add_paragraph()
add_image(doc, IMAGES["activity"], "Figure 5: Activity Diagram — System Workflow", width=Inches(5.5))
doc.add_paragraph()

doc.add_page_break()
add_heading(doc, "5.5 ER Diagram", 2)
add_body(doc, (
    "The ER Diagram shows the actual database schema. "
    "SQL Server stores the Users table (managed by Entity Framework Core). "
    "MongoDB stores Tickets (with embedded TicketMessage conversation thread) and Chats collections. "
    "The Admin account is hardcoded in AuthController and is NOT stored in any database."
))
doc.add_paragraph()
add_image(doc, IMAGES["er"], "Figure 6: Entity Relationship Diagram", width=Inches(6.2))
doc.add_paragraph()

doc.add_page_break()

# ─── SECTION 6: API REFERENCE ────────────────────────────────────────────────
add_heading(doc, "6. API Endpoint Reference", 1)

api_endpoints = [
    ("POST",  "/api/auth/login",         "None",   "Login. Returns JWT token + role + user info."),
    ("POST",  "/api/auth/register",      "None",   "Register new user. Hashes password, saves to SQL Server."),
    ("POST",  "/api/auth/logout",        "JWT",    "Logout (client clears token)."),
    ("POST",  "/api/chat",               "JWT",    "Send message to AI. Returns reply + category + sentiment + confidence + ticketCreated."),
    ("GET",   "/api/chat?email=x",       "JWT",    "Get last 50 chat messages for user."),
    ("GET",   "/api/ticket",             "JWT",    "Get all tickets from MongoDB."),
    ("GET",   "/api/ticket/{id}",        "JWT",    "Get single ticket by MongoDB ObjectId."),
    ("GET",   "/api/ticket/analytics",   "JWT",    "Get analytics: counts, trend, category breakdown, avg resolution time."),
    ("POST",  "/api/ticket",             "JWT",    "Manually create a ticket."),
    ("PUT",   "/api/ticket/{id}",        "JWT",    "Admin: update status + append admin reply to Messages[]."),
    ("POST",  "/api/ticket/{id}/reply",  "JWT",    "User: append follow-up reply to Messages[] thread."),
    ("POST",  "/predict (ML :5001)",     "None",   "ML: classify category + sentiment with confidence scores."),
    ("POST",  "/retrain (ML :5001)",     "None",   "ML: force delete .pkl files and retrain from CSV datasets."),
    ("GET",   "/health (ML :5001)",      "None",   "ML: health check, returns loaded model classes."),
]

table = doc.add_table(rows=1, cols=4)
table.style = "Table Grid"
headers = ["Method", "Endpoint", "Auth", "Description"]
for i, h in enumerate(headers):
    c = table.rows[0].cells[i]
    c.text = h
    for run in c.paragraphs[0].runs:
        run.bold = True
    set_cell_bg(c, "1a237e")
    for run in c.paragraphs[0].runs:
        run.font.color.rgb = RGBColor(255,255,255)

for method, ep, auth, desc in api_endpoints:
    row = table.add_row()
    row.cells[0].text = method
    row.cells[1].text = ep
    row.cells[2].text = auth
    row.cells[3].text = desc
    for cell in row.cells:
        for run in cell.paragraphs[0].runs:
            run.font.size = Pt(9.5)

doc.add_page_break()

# ─── SECTION 7: DATABASE DESIGN ──────────────────────────────────────────────
add_heading(doc, "7. Database Design", 1)

add_heading(doc, "7.1 SQL Server — Users Table", 2)
table = doc.add_table(rows=1, cols=4)
table.style = "Table Grid"
for i, h in enumerate(["Column", "Type", "Constraints", "Description"]):
    c = table.rows[0].cells[i]
    c.text = h
    for run in c.paragraphs[0].runs:
        run.bold = True
    set_cell_bg(c, "1565c0")
    for run in c.paragraphs[0].runs:
        run.font.color.rgb = RGBColor(255,255,255)

users_cols = [
    ("UserId",       "int",          "PK, Auto-increment",   "Unique user identifier"),
    ("FullName",     "varchar(100)", "NOT NULL",              "User's full name"),
    ("Email",        "varchar(100)", "UNIQUE, NOT NULL",      "User's email address"),
    ("PasswordHash", "varchar(64)",  "NOT NULL",              "SHA-256 hashed password"),
    ("Role",         "varchar(10)",  "DEFAULT 'User'",        "User role: 'User' only (Admin hardcoded)"),
    ("CreatedAt",    "datetime",     "DEFAULT NOW()",         "Account creation timestamp"),
]
for col in users_cols:
    row = table.add_row()
    for i, val in enumerate(col):
        row.cells[i].text = val
        for run in row.cells[i].paragraphs[0].runs:
            run.font.size = Pt(10)

doc.add_paragraph()
add_body(doc, "Note: Admin account (admin@support.com / Admin@123) is hardcoded in AuthController.cs and is NOT stored in SQL Server.", bold=False)
doc.add_paragraph()

add_heading(doc, "7.2 MongoDB — Tickets Collection", 2)
table2 = doc.add_table(rows=1, cols=3)
table2.style = "Table Grid"
for i, h in enumerate(["Field", "Type", "Description"]):
    c = table2.rows[0].cells[i]
    c.text = h
    for run in c.paragraphs[0].runs:
        run.bold = True
    set_cell_bg(c, "2e7d32")
    for run in c.paragraphs[0].runs:
        run.font.color.rgb = RGBColor(255,255,255)

tickets_cols = [
    ("_id",        "ObjectId",              "Auto-generated primary key"),
    ("userEmail",  "string",                "Email of ticket creator (links to SQL Users.Email)"),
    ("message",    "string",                "Original user message / issue description"),
    ("category",   "string",                "ML-classified: Billing | Technical | Account | Shipping | General"),
    ("priority",   "string",                "Derived from sentiment: High (Negative) | Medium (others)"),
    ("status",     "string",                "Default 'Open'. Updated by admin: Open | Resolved | Closed"),
    ("adminReply", "string (nullable)",      "Legacy single reply field (kept for backward compatibility)"),
    ("messages",   "Array[TicketMessage]",   "Conversation thread: list of {sender, text, sentAt} objects"),
    ("createdAt",  "datetime",               "Ticket creation timestamp"),
    ("resolvedAt", "datetime (nullable)",    "Set when status → Resolved or Closed"),
]
for col in tickets_cols:
    row = table2.add_row()
    for i, val in enumerate(col):
        row.cells[i].text = val
        for run in row.cells[i].paragraphs[0].runs:
            run.font.size = Pt(10)

doc.add_paragraph()
add_heading(doc, "7.3 MongoDB — TicketMessage (Embedded in Tickets.messages[])", 2)
add_body(doc, "Each item in the Messages[] array has these fields:")
for item in [
    "sender: string — 'admin' or 'user'",
    "text: string — The reply message content",
    "sentAt: datetime — Timestamp when the message was sent",
]:
    add_bullet(doc, item)

doc.add_paragraph()
add_heading(doc, "7.4 MongoDB — Chats Collection", 2)
for item in [
    "_id: ObjectId — Auto-generated",
    "userEmail: string — Identifies the user",
    "message: string — The user's chat message",
    "reply: string — The AI/FAQ reply",
    "sentiment: string — Positive | Negative | Neutral",
    "createdAt: datetime — Timestamp",
]:
    add_bullet(doc, item)

doc.add_page_break()

# ─── SECTION 8: ML MODELS ────────────────────────────────────────────────────
add_heading(doc, "8. Machine Learning Models", 1)

add_heading(doc, "8.1 Overview", 2)
add_body(doc, (
    "The Python Flask ML API (port 5001) hosts two scikit-learn classification pipelines. "
    "Each pipeline consists of a TF-IDF Vectorizer followed by a Logistic Regression classifier. "
    "Models are trained once and saved as .pkl files; subsequent API restarts load from disk."
))

add_heading(doc, "8.2 Category Classifier", 2)
add_body(doc, "Classifies support messages into 5 categories:", bold=True)
for item in [
    "Billing — payment failures, refunds, invoices, subscriptions",
    "Technical — app errors, crashes, server errors, hardware issues",
    "Account — login problems, password reset, account deletion, 2FA issues",
    "Shipping — delivery delays, tracking, wrong items, returns",
    "General — business hours, policies, general inquiries",
]:
    add_bullet(doc, item)

add_heading(doc, "8.3 Sentiment Classifier", 2)
add_body(doc, "Classifies message tone into 3 sentiments:", bold=True)
for item in [
    "Positive — satisfied customers, general inquiries, appreciation",
    "Negative — frustrated customers, urgent issues, complaints",
    "Neutral — informational questions, status checks",
]:
    add_bullet(doc, item)

add_heading(doc, "8.4 Training Datasets", 2)
table = doc.add_table(rows=1, cols=3)
table.style = "Table Grid"
for i, h in enumerate(["Dataset", "Used For", "Sample Size"]):
    c = table.rows[0].cells[i]
    c.text = h
    for run in c.paragraphs[0].runs:
        run.bold = True
    set_cell_bg(c, "4a148c")
    for run in c.paragraphs[0].runs:
        run.font.color.rgb = RGBColor(255,255,255)

ds = [
    ("twitter_training.csv",                 "Sentiment (Positive/Negative/Neutral)",     "Up to 15,000 rows"),
    ("twitter_validation.csv",               "Sentiment validation",                       "All available rows"),
    ("customer_support_tickets.csv",         "Category + Sentiment (priority-derived)",    "Up to 3,000 rows"),
    ("all_tickets_processed_improved_v3.csv","Category (IT Helpdesk topics mapped)",       "Up to 5,000 rows"),
    ("Synthetic examples (hardcoded)",       "Category (30 examples per class)",           "150 rows"),
]
for d in ds:
    row = table.add_row()
    for i, val in enumerate(d):
        row.cells[i].text = val
        for run in row.cells[i].paragraphs[0].runs:
            run.font.size = Pt(10)

doc.add_paragraph()
add_heading(doc, "8.5 Pipeline Configuration", 2)
for item in [
    "TfidfVectorizer: max_features=30,000 | ngram_range=(1,2) | sublinear_tf=True | min_df=2",
    "LogisticRegression: max_iter=1000 | C=5.0 | solver='lbfgs' | multi_class='auto'",
    "Train/Test split: 85% / 15% with stratification",
    "Text preprocessing: lowercase → remove URLs → remove @mentions → remove punctuation → collapse whitespace",
]:
    add_bullet(doc, item)

doc.add_page_break()

# ─── SECTION 9: SECURITY ─────────────────────────────────────────────────────
add_heading(doc, "9. Security", 1)

security_items = [
    ("Password Storage",       "SHA-256 hashed. Plain text passwords are never stored in the database."),
    ("Authentication",         "JWT Bearer tokens using HMAC-SHA256 signing. Secret key stored in appsettings.json."),
    ("Token Validation",       "Every protected endpoint validates: token signature, expiry lifetime. Issuer/Audience not validated (development config)."),
    ("Role-Based Access",      "ProtectedRoute: requires any valid JWT. AdminRoute: requires JWT with role='Admin'."),
    ("CORS Policy",            "Restricted to http://localhost:3000 only. No wildcard origins allowed."),
    ("Admin Credentials",      "Hardcoded in AuthController.cs. Not in database, reducing DB compromise risk."),
    ("API Authorization",      "All controllers marked [Authorize]. Auth endpoints (/login, /register) are public."),
    ("Input Validation",       "Empty messages rejected at controller level. Email uniqueness enforced in SQL Server."),
    ("Gemini API Key",         "Stored in appsettings.json. Should be moved to environment variable for production."),
]

table = doc.add_table(rows=1, cols=2)
table.style = "Table Grid"
for i, h in enumerate(["Security Feature", "Implementation"]):
    c = table.rows[0].cells[i]
    c.text = h
    for run in c.paragraphs[0].runs:
        run.bold = True
    set_cell_bg(c, "b71c1c")
    for run in c.paragraphs[0].runs:
        run.font.color.rgb = RGBColor(255,255,255)
for feat, impl in security_items:
    row = table.add_row()
    row.cells[0].text = feat
    row.cells[1].text = impl
    for cell in row.cells:
        for run in cell.paragraphs[0].runs:
            run.font.size = Pt(10)

doc.add_page_break()

# ─── SECTION 10: ASSUMPTIONS & DEPENDENCIES ──────────────────────────────────
add_heading(doc, "10. Assumptions & Dependencies", 1)

add_heading(doc, "10.1 Assumptions", 2)
for item in [
    "Python Flask ML API must be started BEFORE the .NET API handles chat requests.",
    "MongoDB must be running locally on port 27017 before the .NET API starts.",
    "SQL Server LocalDB is available (included with Visual Studio).",
    "A valid Google Gemini API key is configured in appsettings.json.",
    "The system is run in a development environment (localhost). Production deployment would require additional configuration.",
    "Users have access to a modern web browser with JavaScript enabled.",
]:
    add_bullet(doc, item)

add_heading(doc, "10.2 Dependencies", 2)

deps = [
    ("React 18",            "Frontend SPA framework"),
    ("React Router v6",     "Client-side routing"),
    ("ASP.NET Core 10",     "REST API framework"),
    ("Entity Framework Core","SQL Server ORM"),
    ("MongoDB.Driver (C#)", "MongoDB client for .NET"),
    ("Microsoft.IdentityModel.Tokens", "JWT validation"),
    ("Flask",               "Python web framework for ML API"),
    ("scikit-learn",        "TF-IDF + Logistic Regression ML models"),
    ("pandas",              "CSV dataset loading"),
    ("joblib",              "Model serialization (.pkl)"),
    ("flask-cors",          "CORS headers for ML API"),
    ("Google Gemini API",   "External LLM for AI chatbot replies"),
]
table = doc.add_table(rows=1, cols=2)
table.style = "Table Grid"
for i, h in enumerate(["Package / Service", "Purpose"]):
    c = table.rows[0].cells[i]
    c.text = h
    for run in c.paragraphs[0].runs:
        run.bold = True
    set_cell_bg(c, "37474f")
    for run in c.paragraphs[0].runs:
        run.font.color.rgb = RGBColor(255,255,255)
for pkg, purpose in deps:
    row = table.add_row()
    row.cells[0].text = pkg
    row.cells[1].text = purpose
    for cell in row.cells:
        for run in cell.paragraphs[0].runs:
            run.font.size = Pt(10)

# ─── SAVE ─────────────────────────────────────────────────────────────────────
doc.save(OUTPUT_DOCX)
print(f"\nSRS document saved to:\n  {OUTPUT_DOCX}\n")
