FROM microsoft/dotnet:2.1-aspnetcore-runtime
WORKDIR /var/www
COPY pub .
EXPOSE 5000
ENTRYPOINT ["dotnet", "EmailAuth.dll"]
