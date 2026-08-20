rem this needs to be run using a start /wait command or else the web service will fail.  
exit 0
cd
cd %~p0
cd 
Lyt.Translator.Cli.exe PppLanguages.json
cd 
exit 0
