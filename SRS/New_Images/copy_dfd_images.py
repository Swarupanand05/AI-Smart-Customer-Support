"""
Copy newly generated DFD images to New_Images folder.
Run: python copy_dfd_images.py
"""
import shutil, os

SRC = r"C:\Users\bswar\.gemini\antigravity-ide\brain\d1b98148-18f6-4004-81de-38830c5580be"
DST = r"d:\Mega Project\AI-Smart-Customer-Support\SRS\New_Images"
os.makedirs(DST, exist_ok=True)

files = {
    "dfd_level0_context_1785550344677.png": "DFD_Level0_Context_Diagram.png",
    "dfd_level1_main_1785550370256.png":    "DFD_Level1_Main_Processes.png",
    "dfd_level2_detailed_1785550398566.png":"DFD_Level2_AI_Processing_Decomposition.png",
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

print(f"\n📁 DFDs saved to:\n   {DST}")
