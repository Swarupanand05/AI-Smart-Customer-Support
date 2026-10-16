import os
from PIL import Image

def convert_to_pdf():
    # Paths
    base_dir = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    image_path = os.path.join(base_dir, "Architecture diagram.jpeg")
    pdf_path = os.path.join(base_dir, "Architecture_diagram.pdf")
    
    if not os.path.exists(image_path):
        # Try parent directory or current directory fallback
        image_path = os.path.abspath("../Architecture diagram.jpeg")
        pdf_path = os.path.abspath("../Architecture_diagram.pdf")
        
    if not os.path.exists(image_path):
        # Try local path
        image_path = "Architecture diagram.jpeg"
        pdf_path = "Architecture_diagram.pdf"
        
    print(f"Looking for block diagram at: {os.path.abspath(image_path)}")
    
    if os.path.exists(image_path):
        try:
            image = Image.open(image_path)
            # Convert to RGB mode (PDF requires RGB, some jpegs can be RGBA or CMYK)
            rgb_image = image.convert('RGB')
            rgb_image.save(pdf_path, "PDF", resolution=100.0)
            print(f"Success! stand-alone PDF saved at: {os.path.abspath(pdf_path)}")
        except Exception as e:
            print(f"Error during conversion: {e}")
    else:
        print("Error: 'Architecture diagram.jpeg' not found. Please place it in the same directory.")

if __name__ == "__main__":
    convert_to_pdf()
