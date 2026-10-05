"""Deterministic display hierarchy, not a historical capacity classification."""
import heapq, math
from collections import defaultdict

def rank(features):
    # Source vertices define connectivity; 100 m bins absorb minor digitizing gaps.
    # No inferred joins at crossing interiors and no fabricated geometry.
    graph=defaultdict(list); edges=[]
    for i,f in enumerate(features):
        g=f['geometry']; lines=[g['coordinates']] if g['type']=='LineString' else g['coordinates']
        for line in lines:
            for start,end in zip(line,line[1:]):
                a=tuple(round(v*10) for v in start); b=tuple(round(v*10) for v in end)
                length=math.dist(start,end)
                if a==b: continue
                e=len(edges);edges.append((a,b,length,i))
                graph[a].append((b,length,e));graph[b].append((a,length,e))
    # Regional hubs: one well-connected endpoint in each 250 km geographic cell.
    tiles=defaultdict(list)
    for n in graph: tiles[(math.floor(n[0]/2500),math.floor(n[1]/2500))].append(n)
    hubs=sorted(min(nodes,key=lambda n:(-len(graph[n]),n)) for nodes in tiles.values())
    major=set()
    for hub in hubs:
        targets=set(sorted((n for n in hubs if n!=hub and math.dist(n,hub)<=8000),key=lambda n:(math.dist(n,hub),n))[:4])
        dist={hub:0}; prev={}; queue=[(0,hub)]
        while queue and targets:
            d,n=heapq.heappop(queue)
            if d!=dist[n]:continue
            if n in targets:
                targets.remove(n); cur=n
                while cur!=hub:
                    last,e=prev[cur];major.add(edges[e][3]);cur=last
            if d>1600:continue
            for other,w,e in graph[n]:
                nd=d+w
                if nd<dist.get(other,float('inf')):
                    dist[other]=nd;prev[other]=(n,e);heapq.heappush(queue,(nd,other))
    # Peel terminal branches up to 60 km from their tips for the intermediate view.
    degree={n:len(v) for n,v in graph.items()}; removed=set(); queue=[]
    for n,d in degree.items():
        if d==1:heapq.heappush(queue,(0,n))
    while queue:
        depth,n=heapq.heappop(queue)
        for other,w,e in graph[n]:
            if e in removed or depth+w>60:continue
            removed.add(e);degree[n]-=1;degree[other]-=1
            if degree[other]==1:heapq.heappush(queue,(depth+w,other))
    regional={edge[3] for e,edge in enumerate(edges) if e not in removed}
    for i,f in enumerate(features):
        f['properties']['displayTier']=0 if i in major else 1 if i in regional else 2
    return {'method':'regional-hub-shortest-paths-v1','historicalClassification':False,
      'hubCellKm':250,'endpointBinKm':0.1,'nearbyHubs':4,'hubSearchRadiusKm':800,
      'regionalTerminalPruningKm':60,'counts':[sum(f['properties']['displayTier']==t for f in features) for t in range(3)],
      'limitations':'Display-only approximation. Source endpoint bins may omit real connections or merge nearby endpoints; crossings are not joined. Hubs are geometric representatives, not verified historical stations. No military capacity or service inference.'}
