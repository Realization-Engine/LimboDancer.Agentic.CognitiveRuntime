"""Derive a generalized circa-2000 tree-cover layer, not historical forest boundaries."""
import hashlib,json,zipfile
from pathlib import Path
import numpy as np
import rasterio
from rasterio.windows import from_bounds
from rasterio.warp import reproject,Resampling
from rasterio.transform import from_origin
from rasterio.features import shapes
from shapely.geometry import shape,mapping
from shapely.ops import transform
from pyproj import Transformer
ROOT=Path(__file__).resolve().parent
src=ROOT/"sources"
with zipfile.ZipFile(src/"glc2000.zip") as z:
    target=src/"glc2000.tif"
    if not target.exists():
        with z.open("Tiff/glc2000_v1_1.tif") as i,target.open("wb") as o:
            import shutil
            shutil.copyfileobj(i,o)
with rasterio.open(target) as ds:
    window=from_bounds(-40,20,65,85,ds.transform).round_offsets().round_lengths()
    raw=ds.read(1,window=window)
    trees=np.where((raw>=1)&(raw<=8),1.,np.where((raw>=1)&(raw<=22),0.,-1.)).astype("float32")
    dst=np.full((460,520),-1,dtype="float32")
    grid=from_origin(1921000,5710000,10000,10000)
    reproject(trees,dst,src_transform=ds.window_transform(window),src_crs="EPSG:4326",
              src_nodata=-1,dst_transform=grid,dst_crs="EPSG:3035",dst_nodata=-1,resampling=Resampling.average)
inverse=Transformer.from_crs("EPSG:3035","EPSG:4326",always_xy=True).transform
features=[]
mask=(dst>=.4).astype("uint8")
for g,v in shapes(mask,mask=mask.astype(bool),transform=grid):
    p=shape(g)
    if p.area<200e6:continue
    p=transform(inverse,p.simplify(2000,preserve_topology=True))
    features.append({"type":"Feature","geometry":mapping(p),"properties":{"name":"Tree cover (2000)","source":"JRC GLC2000 v1.1","threshold":.4}})
out=src/"glc2000_forest_europe.geojson"
out.write_text(json.dumps({"type":"FeatureCollection","features":features},separators=(",",":")),encoding="utf8")
manifest=json.loads((src/"manifest.json").read_text(encoding="utf-8-sig"))
for name in ["ne_50m_geography_regions_polys","ne_10m_roads","ne_10m_railroads","glc2000_forest_europe"]:
    manifest["sources"]=[x for x in manifest["sources"] if x["name"]!=name]
    record={"name":name,"sha256":hashlib.sha256((src/(name+".geojson")).read_bytes()).hexdigest()}
    if name.startswith("ne_"):record.update(url=f"https://raw.githubusercontent.com/nvkelso/natural-earth-vector/{manifest['commit']}/geojson/{name}.geojson",license="Public domain")
    else:record.update(url="https://forobs.jrc.ec.europa.eu/glc2000/data",attribution="GLC2000 database, European Commission Joint Research Centre, 2003",sourceYear=2000,
        archiveUrl="https://forobs.jrc.ec.europa.eu/data/products/glc2000/glc2000_v1_1_Tiff.zip",
        archiveSha256=hashlib.sha256((src/"glc2000.zip").read_bytes()).hexdigest(),
        method="Classes 1-8 tree cover; 10 km equal-area average; threshold 40%; patches >=200 km2; 2 km simplification. Classes 9 mosaics and 10 burnt excluded.",
        license="JRC terms apply; see source review. Not Natural Earth public-domain data.")
    manifest["sources"].append(record)
manifest["provider"]="Natural Earth and European Commission JRC"
manifest["license"]="Mixed sources; see individual entries and README attribution"
(src/"manifest.json").write_text(json.dumps(manifest,indent=2),encoding="utf8")
print("Derived",len(features),"generalized tree-cover patches")
