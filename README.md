# Intro
dockerized api for providing name and faculty based professor queriesn for the hsb
# requirements
- docker
- dotnet10.0 or higher (local build only)
# build and run


## docker pull
```
docker pull ghcr.io/finnimon/wbmcs-api:latest
docker run -p 8080:8080 ghcr.io/finnimon/wbmcs-api:latest

```
hosted at http://localhost:8080

## non docker
```bash
cd $sln_dir
dotnet restore 
dotnet build
dotnet run --project ./Wbmcs.Api/Wbmcs.Api.csproj
```
hosted at https://localhost:5131

## docker
```bash
docker build -t wb-api .
docker run -p 8080:8080 wb-api .

```
hosted at http://localhost:8080


# Endpoints

## http GET
### Get all professors
```http
GET http://localhost:8080/professors
```
response:
```json
[
  {
    "title": "Prof. Dr.-Ing. Jorgen von der Brelie",
    "isProfessor": true,
    "position": null,
    "email": "Jorgen.vonderBrelie@hs-bremen.de",
    "phone": "+49 421 5905 5547",
    "mobile": null,
    "image": {
      "src": "/assets/hsb/de/_processed_/8/4/csm_von_der_Brelie_Jorgen-04813_9541211613.jpg",
      "alt": "Auf dem Bild ist Jorgen von der Brelie zu sehen. Er hat kurzes blondes Haar und trägt ein weißes Hemd unter einem blauen Jacket. ",
      "title": "Jorgen von der Brelie"
    },
    "url": "/person/jvonderbrelie/",
    "faculty": [
      "Fakultät Natur und Technik - Abteilung Maschinenbau",
      "Fakultät Natur und Technik"
    ]
  },
  {
    "title": "Prof. Dr. Eva Georg",
    "isProfessor": true,
    "position": "Professorin für Professionalität und Ethik in der Sozialen Arbeit",
    "email": "Eva.Georg@hs-bremen.de",
    "phone": "+49 421 5905 6730",
    "mobile": null,
    "image": null,
    "url": "/person/egeorg/",
    "faculty": [
      "Fakultät Gesellschaftswissenschaften"
    ]
  },...
]
```
### Get total prof count
```http
GET http://localhost:8080/professors/count
```
response: 
```json
210
```

### Get faculty names and sizes
```http
GET http://localhost:8080/professors/faculties
```
response (example): 
```json
[
  {
    "name": "Fakultät Natur und Technik - Abteilung Maschinenbau",
    "count": 22
  },
  {
    "name": "Fakultät Natur und Technik",
    "count": 41
  },...
]
```
## Query professors by name and faculty
faculty and name are optional. faculty must match a faculty name from /professors/faculties exactly. whitespace and " are trimmed. also published as POST endpoint

```http
GET http://localhost:8080/professors/search?faculty=&name=
```

response for 
```http
GET http://localhost:8080/professors/search?name=d kampn
```
:
```json
[
  {
    "title": "Prof. Dr.-Ing. Dennis Kampen",
    "isProfessor": true,
    "position": null,
    "email": "Dennis.Kampen@hs-bremen.de",
    "phone": "+49 421 5905 5420",
    "mobile": null,
    "image": {
      "src": "/assets/hsb/de/_processed_/d/7/csm_Kampen_Dennis-07779-1200px_f66293ee45.jpg",
      "alt": "Auf dem Bild ist Dennis Kampen zu sehen. Er trägt kurzes blondes Haar und ein weißes Hemd mit dunklem Sakko. ",
      "title": "Dennis Kampen"
    },
    "url": "/person/dekampen/",
    "faculty": [
      "Fakultät Elektrotechnik und Informatik"
    ]
  }
]
```
## http POST
## Query professors by name and faculty
faculty and name are optional. faculty must match a faculty name from /professors/faculties exactly. whitespace and " are trimmed.

```http
POST http://localhost:8080/professors/search?faculty=&name=
```

response for 
```http
POST http://localhost:8080/professors/search?name=d kampn
```
:
```json
[
  {
    "title": "Prof. Dr.-Ing. Dennis Kampen",
    "isProfessor": true,
    "position": null,
    "email": "Dennis.Kampen@hs-bremen.de",
    "phone": "+49 421 5905 5420",
    "mobile": null,
    "image": {
      "src": "/assets/hsb/de/_processed_/d/7/csm_Kampen_Dennis-07779-1200px_f66293ee45.jpg",
      "alt": "Auf dem Bild ist Dennis Kampen zu sehen. Er trägt kurzes blondes Haar und ein weißes Hemd mit dunklem Sakko. ",
      "title": "Dennis Kampen"
    },
    "url": "/person/dekampen/",
    "faculty": [
      "Fakultät Elektrotechnik und Informatik"
    ]
  }
]
```
