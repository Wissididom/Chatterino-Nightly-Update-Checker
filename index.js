require('dotenv').config();
const util = require('node:util');
const xml2js = require('xml2js');
const fs = require('fs');
const webhookUsername = 'Chatterino Nightly';
const webhookAvatarUrl = 'https://camo.githubusercontent.com/6ca305d42786c9dbd0b76f5ade013601b080d71a598e881b4349dff2eafae6c7/68747470733a2f2f666f757274662e636f6d2f696d672f63686174746572696e6f2d69636f6e2d36342e706e67';
const changelogLink = 'https://github.com/Chatterino/chatterino2/blob/master/CHANGELOG.md';
const nightlyLink = 'https://github.com/Chatterino/chatterino2/releases/tag/nightly-build';
const contentFormatString = "New Nightly Version (Updated: <t:%d:F>):\nLatest Commit Message: ``%s`` by ``%s``\nChangelog: <%s>\nLink: <%s>";
fetch('https://github.com/Chatterino/chatterino2/releases.atom').then(res => res.text()).then(text => {
	return xml2js.parseStringPromise(text);
}).then(xmlObj => {
	let xmlEntries = xmlObj['feed']['entry'];
	for (let xmlEntry of xmlEntries) {
		if (xmlEntry['title'][0] == 'Nightly Release') {
			let date = new Date();
			date.setDate(date.getDate() - 1);
			let updated = xmlEntry['updated'][0];
			let updatedDate = new Date(updated);
			let timestamp = Math.floor(updatedDate.getTime() / 1000);
			//let differenceSeconds = (new Date() - updatedDate) / 1000;
			//let differenceMinutes = differenceSeconds / 60;
			//let differenceHours = differenceMinutes / 60;
			//console.log(`${differenceSeconds} / 60 = ${differenceMinutes} / 60 = ${differenceHours}`);
			fs.readFile('lastUpdatedValue', async (err, data) => {
				if (!data || data.toString().trim() != updated.trim()) {
					console.log('There is a new version!');
					let latestCommit = await fetch('https://github.com/Chatterino/chatterino2/commits/master.atom').then(res => res.text()).then(text => {
						return xml2js.parseStringPromise(text);
					}).then(xmlCommitObj => {
						return xmlCommitObj['feed']['entry'][0];
					}).catch(err => console.error(err));
					fetch(`${process.env.DISCORD_WEBHOOK_URL}?wait=true`, {
						method: 'POST',
						headers: {
							'Content-Type': 'application/json'
						},
						body: JSON.stringify({
							username: webhookUsername,
							avatar_url: webhookAvatarUrl,
							allowed_mentions: {
								parse: []
							}, // Disable any kind of mention
							content: util.format(contentFormatString, timestamp, latestCommit['title'][0].trim(), latestCommit['author'][0]['name'][0].trim(), changelogLink, nightlyLink)
						})
					}).then(async res => {
						if (!res.ok) {
							console.error(await res.text());
						}
					}).catch(err => console.error(err));
				} else {
					console.log('Already latest version!');
				}
				fs.writeFile('lastUpdatedValue', updated, err => {
					if (err) throw err;
					console.log('Saved lastUpdatedValue!');
				});
			});
		}
	}
}).catch(err => console.error(err));
