# BubbleBot

`BubbleBot` est une boîte à outils d'automatisation en `.NET 9`, pensée principalement pour Windows et construite autour de l'écosystème Dofus 3.

Le dépôt contient :

- un bot principal de jeu avec tableau de bord console temps réel
- des utilitaires de connexion et de gestion de comptes
- des outils liés à l'abonnement et au shield
- des outils d'orchestration par proxy et par lots
- des bibliothèques partagées pour le protocole, les données et le combat

Ce dépôt est publié sous licence `GPL-3.0`. Il n'est pas affilié à Ankama.

## État du projet

Le code est utilisable, mais il reste fortement orienté vers un environnement Windows réel :

- il attend une installation locale de Dofus
- il lit des données du launcher Ankama dans `%AppData%`
- plusieurs exécutables reposent sur des fichiers texte/json présents dans le dossier de travail
- certaines parties sont très dépendantes de l'environnement d'exploitation

Si vous souhaitez réutiliser ou publier ce dépôt, prévoyez d'adapter les chemins, la gestion des secrets et les workflows à votre propre contexte.

## Fonctionnalités principales

- Orchestration multi-comptes depuis une seule console
- Gestion de session jeu, login, monde et bascule vers serveur de combat
- Workflows de chasse au trésor, déplacement, Zaap, banque et groupe
- Suivi d'état de combat et helpers autour des sorts
- Notifications via Discord/webhooks
- Outils de connexion compatibles proxy
- Lanceurs batch pour création de comptes, connexion, abonnement et split de déploiement

## Structure du dépôt

```text
BubbleBot.sln
|-- BubbleBot.Cli/                    Runtime principal et logique d'automatisation en jeu
|-- BubbleBot.Connect/                Connexion ou mise à jour des comptes launcher
|-- BubbleBot.ConnectStarter/         Lanceur batch de BubbleBot.Connect
|-- BubbleBot.AccountCreation/        Workflow de création de compte
|-- BubbleBot.AccountCreationStarter/ Lanceur batch de création de comptes
|-- BubbleBot.Subscribe/              Workflow d'abonnement
|-- BubbleBot.SubscriberStarter/      Lanceur batch d'abonnement
|-- BubbleBot.ShieldDisable/          Utilitaire de désactivation du shield
|-- BubbleBot.ProxyAssociate/         Outil d'association comptes/proxys
|-- BubbleBot.Split/                  Découpe un dossier de travail en plusieurs shards
|-- BubbleBot.FrigostAdapter/         Export vers un format JSON compatible Frigost
`-- libs/                             Bibliothèques partagées core, datacenter, protocole, damage, générateurs
```

## Vue d'ensemble de l'architecture

Le runtime principal se trouve dans [BubbleBot.Cli/BubbleBot.Cli.csproj](BubbleBot.Cli/BubbleBot.Cli.csproj).

Le flux global est le suivant :

1. `BubbleBot.Cli/Program.cs` initialise le datacenter, les repositories, le pathfinding monde et le tableau de bord console.
2. `BotManager` charge les comptes, résout les proxys, s'authentifie auprès d'Ankama et crée un `BotClient` par compte.
3. `BotClient` gère la connexion gateway/auth puis transfère la main à `BotGameClient`.
4. `BotGameClient` porte l'état de session monde et délègue la logique aux services :
   - transport et vérification
   - navigation et lifecycle de map
   - chasse au trésor
   - orchestration métier
   - handlers de messages regroupés par domaine
5. `BotKoliClient` gère la session dédiée au serveur de combat lors d'une bascule Koli/arena.

Le bot charge aussi plusieurs repositories locaux :

- items
- sorts
- effets
- métadonnées serveurs
- maps et world graph
- métadonnées d'alliances
- indices de chasse au trésor

## Prérequis

- Windows
- `.NET SDK 9.0`
- une installation locale de Dofus
- des données Ankama Launcher disponibles dans `%AppData%\zaap`
- un accès réseau et des comptes/proxys valides selon les workflows utilisés

### Résolution du client Dofus

Le bot principal essaie de retrouver le client Dofus depuis :

`%AppData%\zaap\repositories\production\dofus\dofus3\release.json`

Si ce fichier n'existe pas ou n'est pas lisible, le code retombe actuellement sur un chemin codé en dur dans [BubbleBot.Cli/Program.cs](BubbleBot.Cli/Program.cs). Sur une autre machine, il faut donc adapter ce fallback.

## Build

Depuis la racine du dépôt :

```powershell
dotnet restore BubbleBot.sln
dotnet build BubbleBot.sln -c Release
```

Le projet principal compile correctement, mais la solution émet encore des warnings existants, notamment des warnings `NU1902` / `NU1903` liés à `SixLabors.ImageSharp` dans `Bubble.Core.Unity`.

## Démarrage rapide

### 1. Compiler la solution

```powershell
dotnet build BubbleBot.sln -c Release
```

### 2. Préparer les fichiers de travail

Le bot principal s'attend à trouver, selon le mode utilisé :

- `accounts.json`
- `accounts.txt`
- `proxies.txt`
- `Trajets/*.json`
- éventuellement des fichiers marqueurs comme `autoopen`, `special`, `emptytobank`, `zaap`

### 3. Lancer le bot principal

```powershell
dotnet run --project BubbleBot.Cli
```

Au démarrage, le bot :

- initialise le datacenter et les repositories
- charge les comptes depuis `%AppData%\zaap\Settings`
- fusionne les métadonnées locales depuis `accounts.json`
- ouvre un dashboard Spectre.Console en temps réel
- démarre les boucles de connexion et de reconnexion pour les comptes avec `toLoad = true`

## Fichiers de runtime

### Fichiers du launcher

Le dépôt s'intègre avec l'état local du launcher Ankama via `%AppData%` :

- `%AppData%\zaap\Settings`
- `%AppData%\zaap\keydata`

Plusieurs utilitaires lisent ou écrivent directement dans ces fichiers.

### Fichiers de travail locaux

#### `accounts.json`

Ce fichier contient les métadonnées BubbleBot ajoutées au-dessus des comptes launcher.

Il est chargé par [BubbleBot.Cli/Services/AccountService.cs](BubbleBot.Cli/Services/AccountService.cs).

Exemple minimal :

```json
[
  {
    "id": 123456,
    "hardwareId": "HWID-EXAMPLE-001",
    "server": 1,
    "username": "account@example.com",
    "toLoad": true,
    "isBank": false,
    "isKoli": false,
    "proxy": "socks5://127.0.0.1:1080@login:password",
    "trajet": "farm-route",
    "autoPass": false
  }
]
```

Champs importants :

- `id` : identifiant compte/launcher
- `hardwareId` : identifiant matériel logique utilisé avant hash
- `server` : identifiant du serveur de jeu
- `username` : login ou label du compte
- `toLoad` : indique si le compte doit être lancé par `BubbleBot.Cli`
- `isBank` : compte utilisé en mode banque
- `isKoli` : compte orienté Koli/arena
- `proxy` : proxy SOCKS5 utilisé par le bot principal
- `trajet` : identifiant de route résolu depuis `Trajets/<id>.json`
- `autoPass` : auto-pass par défaut en combat

#### `accounts.txt`

Utilisé par plusieurs exécutables annexes comme source d'entrée simple.

Selon l'outil, chaque ligne peut ressembler à :

```text
login@example.com:password
```

ou :

```text
socks5://127.0.0.1:1080@login:password login@example.com:password
```

ou :

```text
server1:login@example.com:password
```

#### `proxies.txt`

Utilisé par les outils de connexion, d'abonnement et d'orchestration.

Format le plus courant :

```text
socks5://127.0.0.1:1080@login:password
```

Attention : certains utilitaires attendent un autre format. Voir la section dédiée à chaque exécutable.

#### `Trajets/<nom>.json`

Définition de route chargée par [BubbleBot.Cli/Repository/TrajetRepository.cs](BubbleBot.Cli/Repository/TrajetRepository.cs).

Exemple :

```json
{
  "auto_fight": true,
  "min_monsters": 1,
  "max_monsters": 8,
  "min_groups_players": 0,
  "max_groups_players": 0,
  "items_to_keep": [15263],
  "closest_zaap": { "x": 5, "y": -18 },
  "maps": [
    { "x": 5, "y": -18 },
    { "x": 6, "y": -18 },
    { "x": 6, "y": -17 }
  ]
}
```

### Fichiers marqueurs optionnels

Le bot principal active certains comportements selon la présence de fichiers vides dans le dossier courant :

- `autoopen` ou `autoopen.txt` : ouvre/utilise automatiquement les objets utilisables
- `special` ou `special.txt` : active un comportement de logs/webhooks spécial
- `emptytobank` ou `emptytobank.txt` : active le workflow de vidage vers la banque
- `zaap` ou `zaap.txt` : active le mode Zaap

## Exécutables

### BubbleBot.Cli

Bot principal de jeu.

Lancement :

```powershell
dotnet run --project BubbleBot.Cli
```

Responsabilités :

- bootstrap des repositories et du pathfinding
- chargement des comptes launcher + métadonnées locales
- authentification Ankama
- connexion des comptes au serveur de jeu
- boucles de supervision et de reconnexion
- affichage d'un tableau de bord live

### BubbleBot.Connect

Connecte un compte via les flows Ankama et persiste les données côté launcher.

Lancement :

```powershell
dotnet run --project BubbleBot.Connect -- "login@example.com:password"
```

ou via variable d'environnement :

```powershell
$env:BUBBLE_CONNECT_ACCOUNT="login@example.com:password"
dotnet run --project BubbleBot.Connect
```

Entrées :

- `accounts.txt`
- `proxies.txt`
- répertoires Ankama Launcher sous `%AppData%\zaap`

But :

- effectuer la connexion
- rafraîchir les fichiers `keydata`
- mettre à jour `%AppData%\zaap\Settings`
- attacher ou mettre à jour les métadonnées proxy d'un compte

### BubbleBot.ConnectStarter

Lanceur batch séquentiel pour `BubbleBot.Connect`.

Lancement :

```powershell
dotnet run --project BubbleBot.ConnectStarter
```

Entrée :

- `accounts.txt`

Comportement :

- lance `BubbleBot.Connect.exe` pour chaque ligne
- attend la fin du process avant de passer au suivant

### BubbleBot.AccountCreation

Workflow de création de compte.

Lancement :

```powershell
dotnet run --project BubbleBot.AccountCreation -- "<resetUrl>" "<host:port:user:password>"
```

Variables d'environnement équivalentes :

- `BUBBLE_ACCOUNT_CREATION_RESET_URL`
- `BUBBLE_ACCOUNT_CREATION_PROXY`

Ce projet dépend également des services internes de mail/captcha présents dans le repo.

Sortie :

- ajoute les identifiants créés dans `accounts.txt`

### BubbleBot.AccountCreationStarter

Lanceur batch infini pour la création de comptes, avec un thread par entrée proxy configurée.

Fichier de configuration :

`account-creation-starter.txt`

Format :

```text
<resetUrl> <host:port:user:password>
```

Lancement :

```powershell
dotnet run --project BubbleBot.AccountCreationStarter
```

### BubbleBot.Subscribe

Utilitaire d'abonnement.

Lancement :

```powershell
dotnet run --project BubbleBot.Subscribe -- "login@example.com:password" paysafecard
```

Variable d'environnement :

- `BUBBLE_SUBSCRIBE_ACCOUNT`

Entrées :

- `proxies.txt`
- éventuellement `subscribeCache.json`

Comportement :

- se connecte au compte
- applique le mode d'abonnement choisi
- met à jour `subscribeCache.json`

### BubbleBot.SubscriberStarter

Lanceur batch séquentiel pour `BubbleBot.Subscribe`.

Lancement :

```powershell
dotnet run --project BubbleBot.SubscriberStarter
```

Entrées :

- `accounts.txt`
- éventuellement `paysafecard.txt`

Comportement :

- utilise `kamas` par défaut
- bascule en `paysafecard` si `paysafecard.txt` existe

### BubbleBot.ShieldDisable

Utilitaire de désactivation du shield pour un compte existant.

Lancement :

```powershell
dotnet run --project BubbleBot.ShieldDisable -- "<resetUrl>" "<host:port:user:password>" "<email>" "<password>"
```

Variables d'environnement équivalentes :

- `BUBBLE_SHIELD_DISABLE_RESET_URL`
- `BUBBLE_SHIELD_DISABLE_PROXY`
- `BUBBLE_SHIELD_DISABLE_EMAIL`
- `BUBBLE_SHIELD_DISABLE_PASSWORD`

Sortie :

- ajoute les identifiants dans `accounts.txt`

### BubbleBot.ProxyAssociate

Associe des proxys à des comptes et écrit le résultat dans `output.txt`.

Lancement :

```powershell
dotnet run --project BubbleBot.ProxyAssociate
```

Entrées :

- `accounts.txt`
- `proxies.txt`

### BubbleBot.Split

Découpe le jeu de travail courant en plusieurs dossiers et lance un `BubbleBot.Cli.exe` par shard.

Lancement :

```powershell
dotnet run --project BubbleBot.Split
```

Comportement :

- charge les comptes via `AccountService`
- regroupe les comptes par lots de 25
- copie les fichiers de travail locaux dans des dossiers numérotés
- lance `BubbleBot.Cli.exe` dans chaque dossier généré

### BubbleBot.FrigostAdapter

Convertit les comptes/proxys locaux vers un export JSON au format Frigost.

Lancement :

```powershell
dotnet run --project BubbleBot.FrigostAdapter
```

Entrées :

- `accounts.txt`
- `proxies.txt`

Sortie :

- `accounts.json`

## Notes de développement

### Zones principales du code

- [BubbleBot.Cli/Program.cs](BubbleBot.Cli/Program.cs) : bootstrap, boucles de supervision, dashboard live
- [BubbleBot.Cli/BotManager.cs](BubbleBot.Cli/BotManager.cs) : chargement des comptes, proxys, lifecycle des clients
- [BubbleBot.Cli/BotClient.cs](BubbleBot.Cli/BotClient.cs) : client auth/gateway
- [BubbleBot.Cli/BotGameClient.cs](BubbleBot.Cli/BotGameClient.cs) : façade du client monde/jeu
- [BubbleBot.Cli/BotKoliClient.cs](BubbleBot.Cli/BotKoliClient.cs) : client dédié au serveur de combat
- [BubbleBot.Cli/Services/Clients/Game/](BubbleBot.Cli/Services/Clients/Game) : transport, routing, workflow, navigation, treasure hunt
- [BubbleBot.Cli/Repository/](BubbleBot.Cli/Repository) : maps, sorts, items, effets, routes

### Fichiers de données copiés à l'exécution

Le projet CLI marque les fichiers suivants en copy-to-output :

- `Data/clues.json`
- `Data/dofuspourlesnoobs_clues.json`
- `Data/game_mappings.json`
- `Data/maps.json`
- `Data/alliances.json`

### Logs

Le bot écrit des logs sous `logs/` et peut aussi envoyer des notifications Discord/webhooks selon les fichiers de configuration présents localement et le mode actif.

## Licence

Ce dépôt est distribué sous `GNU GPL v3`. Voir [LICENSE](LICENSE).
