"""Reference overlays; no historical routing or terrain rules are implied."""
from shapely.geometry import shape,mapping,box
from shapely.ops import transform,unary_union
from shapely import make_valid,STRtree
def enrich(cells,vector,land,lakes,api):
    entries=[]; labels=[]
    specs=[("mountains","ne_50m_geography_regions_polys"),("mountains","africa_ranges"),("forest","glc2000_forest_europe"),
           ("roads","ne_10m_roads"),("railways","ne_10m_railroads")]
    dry=land.difference(lakes)
    for layer,name in specs:
        for index,f in enumerate(api.read(name)):
            p=f["properties"]
            if layer=="mountains" and p.get("FEATURECLA") not in ("Range/mtn","Foothills"):continue
            if layer=="roads" and (p.get("type")!="Major Highway" or p.get("scalerank",99)>6):continue
            if layer=="railways" and p.get("mult_track")!=1:continue
            g=shape(f["geometry"])
            if layer=="mountains" and name=="ne_50m_geography_regions_polys":
                g=g.difference(box(-13,20,36,37.5))
            if not g.intersects(box(-40,10,65,85)):continue
            g=make_valid(transform(api.project,make_valid(g.intersection(box(-40,10,65,85))))).intersection(api.AREA)
            if layer in ("mountains","forest"):g=g.intersection(dry)
            if g.is_empty:continue
            g=g.simplify(1.0,preserve_topology=True)
            title=p.get("NAME_EN") or p.get("name") or p.get("label") or ("Main railway" if layer=="railways" else "Major road")
            identity=str(p.get("NE_ID") or p.get("rwdb_rr_id") or p.get("uident") or index)
            entries.append((layer,g,title))
            status="year-2000 generalized reference" if layer=="forest" else "approximate physical-region outline" if layer=="mountains" else "contemporary reference, not WWII-validated"
            vector.append({"type":"Feature","geometry":mapping(g),"properties":{"layer":layer,"name":title,"id":name+":"+identity,"status":status,"source":name}})
            if layer=="mountains" and g.area>10000:
                pt=g.representative_point();labels.append({"name":title,"x":round(pt.x,3),"y":round(pt.y,3)})
    for layer in ("mountains","forest","roads","railways"):
        items=[(g,n) for l,g,n in entries if l==layer]
        geoms=[g for g,n in items]; tree=STRtree(geoms)
        union=unary_union(geoms) if layer in ("mountains","forest") else None
        for c in cells:
            g=api.polygon(c["q"],c["r"])
            found=[int(i) for i in tree.query(g,predicate="intersects")]
            if layer=="mountains":c["mountainRegions"]=sorted(set(items[i][1] for i in found))
            if layer=="forest":c["forestReferenceFraction"]=round(g.intersection(union).area/g.area,5)
            if layer in ("roads","railways"):c[layer+"ReferenceKm"]=round(sum(g.intersection(items[i][0]).length for i in found),2)
    return labels
