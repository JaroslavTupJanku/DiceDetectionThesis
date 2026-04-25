from pathlib import Path
import xml.etree.ElementTree as ET


def parse_voc_xml(xml_path: Path) -> dict:
    tree = ET.parse(xml_path)
    root = tree.getroot()

    filename = root.findtext("filename")

    size = root.find("size")
    width = int(size.findtext("width"))
    height = int(size.findtext("height"))
    depth = int(size.findtext("depth", default="3"))

    objects = []
    for obj in root.findall("object"):
        class_name = obj.findtext("name")

        bbox = obj.find("bndbox")
        xmin = int(float(bbox.findtext("xmin")))
        ymin = int(float(bbox.findtext("ymin")))
        xmax = int(float(bbox.findtext("xmax")))
        ymax = int(float(bbox.findtext("ymax")))

        objects.append({
            "class_name": class_name,
            "bbox": [xmin, ymin, xmax, ymax],
        })

    return {
        "filename": filename,
        "width": width,
        "height": height,
        "depth": depth,
        "objects": objects,
    }