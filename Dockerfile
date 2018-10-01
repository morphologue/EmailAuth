FROM microsoft/dotnet:2.1-aspnetcore-runtime
WORKDIR /var/www
COPY pub .
EXPOSE 5001
ENTRYPOINT ["dotnet", "EmailAuth.dll"]
