"""
COPY GENERATED IMAGES TO New_Images FOLDER
Run this once to copy all new diagram images.
"""
import shutil, os

SRC = r"C:\Users\bswar\.gemini\antigravity-ide\brain\d1b98148-18f6-4004-81de-38830c5580be"
DST = r"d:\Mega Project\AI-Smart-Customer-Support\SRS\New_Images"

os.makedirs(DST, exist_ok=True)

files = {
    "use_case_diagram_new_1785549778529.png":  "1_Use_Case_Diagram.png",
    "er_diagram_new_1785549802038.png":         "2_ER_Diagram.png",
    "sequence_diagram_new_1785549826722.png":   "3_Sequence_Diagram.png",
    "activity_diagram_new_1785549850447.png":   "4_Activity_Diagram.png",
    "class_diagram_new_1785549883603.png":      "5_Class_Diagram.png",
}

for src_name, dst_name in files.items():
    src = os.path.join(SRC, src_name)
    dst = os.path.join(DST, dst_name)
    if os.path.exists(src):
        shutil.copy2(src, dst)
        size_kb = os.path.getsize(dst) // 1024
        print(f"✅  {dst_name}  ({size_kb} KB)")
    else:
        print(f"❌  Source not found: {src_name}")

print(f"\n📁 All images saved to:\n   {DST}")
