import sys, math
N=int(sys.argv[1]); T=int(sys.argv[2]); out=sys.argv[3]
idx=lambda i,j,k: 1+i+(N+1)*(j+(N+1)*k)
with open(out+'.msh','w') as f:
    f.write("MESH dimension 3 ElemType Hexahedra Nnode 8\nCoordinates\n")
    for k in range(N+1):
        for j in range(N+1):
            for i in range(N+1):
                f.write(f"{idx(i,j,k)} {i/N:.5f} {j/N:.5f} {k/N*2:.5f}\n")
    f.write("end coordinates\nElements\n")
    e=1
    for k in range(N):
        for j in range(N):
            for i in range(N):
                n=[idx(i,j,k),idx(i+1,j,k),idx(i+1,j+1,k),idx(i,j+1,k),idx(i,j,k+1),idx(i+1,j,k+1),idx(i+1,j+1,k+1),idx(i,j+1,k+1)]
                f.write(f"{e} "+" ".join(map(str,n))+" 1\n"); e+=1
    f.write("end elements\n")
with open(out+'.res','w') as f:
    f.write("GiD Post Results File 1.0\n\n")
    for t in range(1,T+1):
        f.write(f'Result "Displacement" "Load Analysis" {t} Vector OnNodes\nComponentNames "X-Displ" "Y-Displ" "Z-Displ"\nValues\n')
        for k in range(N+1):
            for j in range(N+1):
                for i in range(N+1):
                    z=k/N*2; a=0.01*t*z*z
                    f.write(f"{idx(i,j,k)} {a:.5f} {0.3*a:.5f} {0:.5f}\n")
        f.write("End Values\n\n")
        f.write(f'Result "Temperature" "Load Analysis" {t} Scalar OnNodes\nValues\n')
        for k in range(N+1):
            for j in range(N+1):
                for i in range(N+1):
                    x,y,z=i/N,j/N,k/N*2
                    f.write(f"{idx(i,j,k)} {20+10*t*math.sin(3*x)*math.cos(2*y)+15*z:.4f}\n")
        f.write("End Values\n\n")
