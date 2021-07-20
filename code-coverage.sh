function pause(){
 read -s -n 1 -p "Press any key to continue . . ."
 echo ""
}

function lineSeparator(){
 printf "\n"
 printf '%*s\n' "${COLUMNS:-$(tput cols)}" '' | tr ' ' -
 printf "\n"
}

cd ./src
dotnet test --no-build --collect "XPlat Code Coverage" --settings coverlet.runsettings
reportgenerator -reports:**/TestResults/**/coverage.opencover.xml -targetdir:codecoverage  -reporttypes:textSummary
lineSeparator
cat ./codecoverage/Summary.txt
pause
rm -rf codecoverage
rm -rf **/TestResults