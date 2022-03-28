FROM harbor.dev.wiqun.com:8443/dotnet/aspnet:alpine
WORKDIR /app
COPY Output /app/
ENTRYPOINT ["dotnet", "SocketServer.dll"]