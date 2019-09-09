#!/bin/bash

su -c 'createuser --username=postgres --no-superuser --pwprompt quaestur' postgres
su -c 'createdb --username=postgres --owner=quaestur --encoding=UNICODE quaestur' postgres

su -c 'createuser --username=postgres --no-superuser --pwprompt publicus' postgres
su -c 'createdb --username=postgres --owner=publicus --encoding=UNICODE publicus' postgres

su -c 'createuser --username=postgres --no-superuser --pwprompt discourseengagement' postgres
su -c 'createdb --username=postgres --owner=discourseengagement --encoding=UNICODE discourseengagement' postgres

