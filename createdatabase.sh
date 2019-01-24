#!/bin/bash

su -c 'createuser --username=postgres --no-superuser --pwprompt quaestur' postgres
su -c 'createdb --username=postgres --owner=quaestur --encoding=UNICODE quaestur' postgres

