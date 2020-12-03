# README #

1. Для компиляции потребуется MS VS 2015, но можно собрать где угодно, это же исходники.
2. Проект зависит от 

* Awesomium
* Newtonsoft.Json
* NHotkey
* EngineIoClientDotNet - https://github.com/Quobject/EngineIoClientDotNet

3. Остальное криво форкнуто и добавлено в проект (dotIRC, jabber-net) потому что потребовались изменения в коде
4. Для запуска потребуется папка DATA от оригинального крысочата (который качается с http://tworatchat.com) там находятся всякие скины и скрипты чата
5. Для голоса потребуется SpeechPlatformRuntime + Windows 10, тогда будут работать и голосовые оповещения, и управление голосом в играх.

# Для тех кто тут случайно #
Это программа агрегатор чатов, умеет (или должна уметь) читать чаты с разных источников и показывать их в одном месте. Так же есть голосовалка, черные/белые списки. Поддержка скинов и http-server для хостинга чата для любого устройства в сети. Что-то там еще есть, я уже и не помню.
Читает:

* twitch
* goodgame
* peka2tv (formely sc2tv)
* others...

Лицензия: MIT