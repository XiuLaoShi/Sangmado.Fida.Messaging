Commands
------------
nuget setApiKey xxx-xxx-xxxx-xxxx

nuget pack ..\Sangmado.Fida.Messaging\Sangmado.Fida.Messaging.csproj -IncludeReferencedProjects -Symbols -Build -Prop Configuration=Release -OutputDirectory ".\packages"

nuget push .\packages\Sangmado.Fida.Messaging.1.0.0.0.nupkg

