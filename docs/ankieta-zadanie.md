# polling app

demo app. not carrying real task due to lack of proper authorization and identification. main reason for the app is proof of db schema correctness for basic polls.

## concept
### main functions
- demo application as proof on schema correctness and proper utilization
- .net backend api and angular front end
- user's identification by cookie - a visitor automatically becomes last user

#### deployment
- both containerized (angular with nginx)
- example deployment to local docker-desktop cluster

#### side functionality
- api needs rate limiter, graylog logging
- warning threshold 2000 users or 3000 polls, on threshold reached email admin
- lockout threshold 3000 users or 5000 polls, on threshold reached and block new poll/user creation

### functions
#### site's visitor roles
- a site's visitor
- a user

These roles are used in table below

| english | polish |
| --------- | -------- |
| site's visitor can't see polls | |
| site's visitor automatically enters last user context written in cookie | |
| site's visitor can create a user | anonimowy może utworzyć użytkownika |
| site's visitor can see user's list | anonimowy widzi listę użytkowników |
| visitor can click "become user" to enter user context | można wybrać "wejdź w rolę" usera |
| user can lookup user's list | użytkownik możne przejrzeć listę userów |
| user can look up polls assigned to the user | user może przejrzeć listę ankiet w których uczestniczy |
| user can enter assigned poll and edit own answers | user może otworzyć ankietę w której uczestniczy i edytować swoje odpowiedzi |
| user can create polls | user może tworzyć ankiety |
| a poll created by the user has status "owned_by" | ankieta utworzona przez usera otrzymuje status "należy do" tego usera |
| poll's owner can add and edit questions | właściciel ankiety może dodawać i edytować pytania |
| poll's owner can delete a poll | właściciel ankiety może usunąć ankietę |
| poll's owner can read answers answered by all assigned users | właściciel ankiety może przejrzeć wyniki ankiety (odpowiedzi na pytania) |
| poll's owner can assign other users to a poll. then they can read the poll and edit own answers | właściciel ankiety może dodać innych userów (wtedy oni widzą ankietę i  |mogą |  | dodać/edytować odpowiedzi) |
| a poll has questions | ankieta ma pytania |
| a question can have many anwers (one from one assigned user) | pytanie może mieć wiele odpowiedzi |
| a question has index telling order of displaying it on poll's list of questions | pytanie ma indeks kolejności wyświetlania na liście pytań w danej ankiecie |
| a user's answer has last edit date | odpowiedź ma datę edycji |

## db shema
### tables

| polls: |
| --- |
| id |
| created_on |
| modified_on |
| owned_by FK user.id (or is_owner column in user-poll table) |

| questions: |
| --- |
| id |
| id_poll FK poll.id |
| order |
| question |

| answers: |
| --- |
| id |
| id_question FK question.id |
| modified_at |
| answer |

| users: |
| --- |
| id |
| name |
| created_at |

| user-poll: |
| --- |
| id_poll FK poll.id |
| id_user FK user.id |
| is_owner |
