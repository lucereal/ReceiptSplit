import axios from "axios";

export function getClaimedTotal(items) {
    if (!items.length) return;

    let claimedTotal = 0.0;
    for (const item of items) {
        // If the claims array has people, then we should add it to the total
        if (item.claims.length) {
            claimedTotal += item.price;
        }
    }
    return claimedTotal;
}

// Gotta make a dictionary where the key is the user, have the value be a dictionary of values
// a claimedTotal value, items dictionary which has the names and their total.
// last note: Include the tip divided by the total amount of people
export function summarize(users, items, tip) {
    if (!users?.length || !items?.length) return;

    const summaryDict = {};
    let dividedTip = 0.0;
    if (tip) {
        dividedTip = parseFloat((tip / users.length).toFixed(2));
    }
    // Set the initial values for each user, and set tip while we are iterating.
    for (let user of users) {
        summaryDict[user.name] = {
            claimedTotal: 0.0,
            sharedTip: dividedTip,
            claimedItems: []
        }
    }

    for (let item of items) {
        const claimedNumber = item.claims.length;
        if (!claimedNumber) continue; // not claimed by anybody? move on.
        
        // First check the users
        for (let user of item.claims) {
            console.log('-- HelperFunctions.jsx|43 >> summaryDict[user]', summaryDict[user]);
            // Add prices.
            const price = parseFloat((item.price / claimedNumber));
            summaryDict[user].claimedTotal += ((price * 100) / 100);

            // Now populate the items obj.
            summaryDict[user].claimedItems.push({
                name: item.description,
                price: price,
                split: claimedNumber // Number of people split with. 1 means not split.
            })
        }
    }

    return summaryDict;
}

export const copyToClipboard = (ref, setter) => {
    navigator.clipboard.writeText(ref.current.value).then(function() {
        console.log('Async: Copying to clipboard was successful!');
        setter("Copied!");
    }, function(err) {
        console.error('Async: Could not copy text: ', err);
    });
}

export const getUrlId = () => {
    let id = window.location.search;
    if (id.indexOf('=') !== -1) {
        return id.split('=')[1];
    }

    return "";
}

export const addItemForUser = (receipt, item, name, users) => {
    const url = "https://receiptparserservices20230928182301.azurewebsites.net/api/UpdateUserItem?name=Functions";
    const payload = {
        id: receipt,
        userId: getUserIdByName(name, users),
        itemId: item.itemId,
        quantity: 1
    }

    axios.post(url, payload).then(res => {
        return res;
    }).catch(e => {
        return false;
    })
}

function getUserIdByName(name, data) {
    // data is an array of user data objects
    for (let user of data) {
        if (user.name === name) {
            return user.userId;
        }
    }

    return "";
}