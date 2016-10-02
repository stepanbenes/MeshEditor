Write-Host "Copying MeshEditor and fem-format-converter output files to Dropbox folder"
robocopy "C:/Projects/MeshEditor/MeshEditor.UI/bin/Release" "C:/Users/benes/Dropbox/Ubuntu/mesheditor/package/usr/lib/mesheditor" *.dll *.exe *.config /xf *.vshost.exe *.vshost.exe.config
robocopy "C:/Projects/MeshEditor/MeshEditor.FormatConverter/bin/Release" "C:/Users/benes/Dropbox/Ubuntu/mesheditor/package/usr/lib/mesheditor" *.dll *.exe *.config /xf *.vshost.exe *.vshost.exe.config
cp c:/Projects/redsvd/redsvd/x64/Release/redsvd.dll c:/Users/benes/Dropbox/ubuntu/mesheditor/package/usr/lib/mesheditor/redsvd.dll
cp c:/Projects/MeshEditor/linux-deployment/mesheditor/package/usr/lib/mesheditor/config.json c:/Users/benes/Dropbox/ubuntu/mesheditor/package/usr/lib/mesheditor/config.json